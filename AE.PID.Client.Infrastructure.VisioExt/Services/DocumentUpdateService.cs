using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reactive.Subjects;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using AE.PID.Client.Core;
using AE.PID.Client.Core.Exceptions;
using AE.PID.Client.Core.VisioExt;
using AE.PID.Core;
using Microsoft.Office.Interop.Visio;
using Refit;
using Splat;
using Path = System.IO.Path;

namespace AE.PID.Client.Infrastructure.VisioExt;

public class DocumentUpdateService : DisposableBase, IDocumentUpdateService
{
    // 替换静态字典为线程安全结构
    private static ConcurrentDictionary<string, MasterSnapshotDto> _masterDict = [];
    private readonly IApiFactory<IDocumentApi> _apiFactory;

    // 添加 CancellationTokenSource 支持取消
    private readonly CancellationTokenSource _cts = new();

    private readonly SemaphoreSlim _dictLock = new(1, 1);

    private readonly BehaviorSubject<bool> _initialized = new(false);
    private readonly Timer _timer;

    public DocumentUpdateService(IApiFactory<IDocumentApi> apiFactory)
    {
        _apiFactory = apiFactory;
        _timer = new Timer(UpdateMasterCaches, null, TimeSpan.Zero, TimeSpan.FromHours(1));
    }

    public IObservable<bool> Initialized => _initialized;

    public async Task UpdateAsync(string filePath, VisioMaster[]? mastersToUpdate = null)
    {
        if (string.IsNullOrEmpty(filePath)) throw new ArgumentNullException(nameof(filePath));
        if (Path.GetExtension(filePath) != ".vsdx")
            throw new ArgumentException(
                $"{filePath} is not a valid document for document update. Only visio drawing file is valid.");
        if (!File.Exists(filePath)) throw new FileNotFoundException($"{filePath} not exist on the local storage.");

        // convert the file to byte-array content and sent as a byte-array
        // because there is an encrypted system on end user, so directly transfer the file to server will not be able to read in the server side
        var fileBytes = File.ReadAllBytes(filePath);

        try
        {
            // 2025.02.06: 首先尝试更新，如果顺利返回信息，则创建备份文件，然后将结果覆盖文件
            var filePart = new ByteArrayPart(fileBytes, Path.GetFileName(filePath));

            // 2025.02.06: 由于refit不支持在form中传递复杂结构对象的数组，所以在此处将它序列化了。
            Stream? result;
            if (mastersToUpdate == null)
            {
                result = await _apiFactory.Api.Update(filePart);
            }
            else
            {
                var items
                    = mastersToUpdate.Select(x => new MasterDto
                            { Name = x.Name, BaseId = x.Id.BaseId, UniqueId = x.Id.UniqueId })
                        .ToArray();
                var itemsJson = JsonSerializer.Serialize(items);
                result = await _apiFactory.Api.Update(filePart, itemsJson);
            }

            // create a copy of the source file
            var backupPath = Path.ChangeExtension(filePath, ".bak");
            if (File.Exists(backupPath))
                backupPath = Path.Combine(Path.GetDirectoryName(backupPath) ?? string.Empty,
                    Path.GetFileNameWithoutExtension(backupPath) + DateTime.Now.ToString("yyyyMMddhhmmss") + ".bak");
            File.Copy(filePath, backupPath);
            this.Log().Info("Backup created at {BackupPath}", backupPath);

            // overwrite the origin file after a successful update
            using (var fileStream = File.Open(filePath, FileMode.Create, FileAccess.Write))
            {
                await result.CopyToAsync(fileStream);
                fileStream.Close();
            }
        }
        catch (ApiException e) when (e.StatusCode == HttpStatusCode.BadRequest)
        {
            this.Log().Error(e);
            throw new DocumentFailedToUpdateException(e.Message);
        }
        catch (ApiException e)
        {
            this.Log().Error(e);
            throw new NetworkNotValidException();
        }
        catch (HttpRequestException e)
        {
            this.Log().Error(e);
            throw new NetworkNotValidException();
        }
    }

    // <inheritdoc />
    public bool HasUpdate(IEnumerable<MasterSnapshotDto> localMasters)
    {
        return localMasters.Any(local =>
            _masterDict.TryGetValue(local.BaseId, out var toCompare) &&
            toCompare.UniqueId != local.UniqueId
        );
    }

    public List<VisioMaster> GetOutdatedMasters(IVDocument document)
    {
        // 关键：在独立线程上下文中同步等待异步操作
        return Task.Run(async () =>
            {
                try
                {
                    return await GetOutdatedMastersAsync(document).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    this.Log().Error("Failed to get outdated masters", ex);
                    return [];
                }
            })
            .GetAwaiter()
            .GetResult(); // 阻塞当前线程直到任务完成
    }

    // <inheritdoc />
    private async Task<List<VisioMaster>> GetOutdatedMastersAsync(IVDocument document)
    {
        if (_masterDict.IsEmpty)
        {
            await _dictLock.WaitAsync();

            var result = await _apiFactory.Api.GetCurrentSnapshot().ConfigureAwait(false);
            _masterDict = new ConcurrentDictionary<string, MasterSnapshotDto>(result.ToDictionary(t => t.BaseId));
        }

        // get the masters of the document
        var needUpdate = new List<VisioMaster>();
        foreach (var master in document.Masters.OfType<IVMaster>())
        {
            if (!_masterDict.TryGetValue(master.BaseID, out var snapshot)) continue;

            if (master.UniqueID != snapshot.UniqueId)
                needUpdate.Add(new VisioMaster(master.BaseID, master.Name, master.UniqueID));
        }

        return needUpdate;
    }

    public override void Dispose()
    {
        _timer.Dispose();

        base.Dispose();
    }

    private async void UpdateMasterCaches(object state)
    {
        try
        {
            var snapshots = await _apiFactory.Api.GetCurrentSnapshot().ConfigureAwait(false);

            await _dictLock.WaitAsync(_cts.Token);
            try
            {
                // todo: handle this
                var newDict = snapshots.ToDictionary(x => x.BaseId);
                _masterDict = new ConcurrentDictionary<string, MasterSnapshotDto>(newDict);
                _initialized.OnNext(true);
                this.Log().Info($"{_masterDict.Count} master snapshots cached.");
            }
            finally
            {
                _dictLock.Release();
            }
        }
        catch (OperationCanceledException)
        {
            // 忽略取消请求
        }
        catch (ApiException e)
        {
            this.Log().Error("API Error during cache update", e);
            _masterDict.Clear();
        }
        catch (HttpRequestException e)
        {
            this.Log().Error("Network Error during cache update", e);
            _masterDict.Clear();
        }
        catch (Exception ex)
        {
            this.Log().Error(ex);
        }
    }
}