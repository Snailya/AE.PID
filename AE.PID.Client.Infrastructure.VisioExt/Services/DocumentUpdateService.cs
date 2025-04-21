using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
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
    private readonly IApiFactory<IDocumentApi> _apiFactory;
    private readonly object _cacheLock = new();

    private readonly Task _initializeTask;
    private ConcurrentDictionary<string, MasterSnapshotDto> _cache;

    private bool _isDisposed;

    private Timer _timer;

    public DocumentUpdateService(IApiFactory<IDocumentApi> apiFactory)
    {
        _apiFactory = apiFactory;

        _initializeTask = InitializeCacheAsync();
    }

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

    public bool IsObsolete(IVDocument document)
    {
        // 确保初始化完成（同步阻塞）
        _initializeTask.GetAwaiter().GetResult();

        lock (_cacheLock)
        {
            return document.Masters.OfType<IVMaster>()
                .Select(x =>
                    new
                    {
                        BaseId = x.BaseID,
                        UniqueId = x.UniqueID,
                        Name = x.NameU
                    })
                .Any(
                    local =>
                        _cache.TryGetValue(local.BaseId, out var toCompare) &&
                        toCompare.UniqueId != local.UniqueId
                );
        }
    }

    public List<VisioMaster> GetObsoleteMasters(IVDocument document)
    {
        // 确保初始化完成（同步阻塞）
        _initializeTask.GetAwaiter().GetResult();

        lock (_cacheLock)
        {
            // get the masters of the document
            var needUpdate = new List<VisioMaster>();
            foreach (var master in document.Masters.OfType<IVMaster>())
            {
                if (!_cache.TryGetValue(master.BaseID, out var snapshot)) continue;

                if (master.UniqueID != snapshot.UniqueId)
                    needUpdate.Add(new VisioMaster(master.BaseID, master.Name, master.UniqueID));
            }

            return needUpdate;
        }
    }

    private async Task InitializeCacheAsync()
    {
        // 首次加载缓存数据
        _cache = new ConcurrentDictionary<string, MasterSnapshotDto>(
            (await FetchDataFromApiAsync().ConfigureAwait(false)).ToDictionary(x => x.BaseId));

        // 启动定时更新任务（5分钟间隔）
        _timer = new Timer(
            _ => UpdateCacheAsync().ConfigureAwait(false).GetAwaiter().GetResult(),
            null,
            TimeSpan.FromMinutes(5), // 首次延迟
            TimeSpan.FromMinutes(5)); // 后续间隔
    }

    private async Task UpdateCacheAsync()
    {
        if (_isDisposed) return;

        try
        {
            var newData = await FetchDataFromApiAsync().ConfigureAwait(false);
            lock (_cacheLock)
            {
                _cache = new ConcurrentDictionary<string, MasterSnapshotDto>(
                    newData.ToDictionary(x => x.BaseId));
            }
        }
        catch
        {
            // 处理异常，可加入重试逻辑
        }
    }

    private async Task<IEnumerable<MasterSnapshotDto>> FetchDataFromApiAsync()
    {
        return await _apiFactory.Api.GetCurrentSnapshot();
    }

    public override void Dispose()
    {
        base.Dispose();

        _isDisposed = true;
        _timer.Dispose();
    }
}