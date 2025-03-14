using System;
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
    private static IEnumerable<MasterSnapshotDto> _masters = [];
    private readonly IApiFactory<IDocumentApi> _apiFactory;

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

        // convert the file to byte-array content and sent as byte-array
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
            var backup = Path.ChangeExtension(filePath, ".bak");
            if (File.Exists(backup))
                backup = Path.Combine(Path.GetDirectoryName(backup) ?? string.Empty,
                    Path.GetFileNameWithoutExtension(backup) + DateTime.Now.ToString("yyyyMMddhhmmss") + ".bak");
            File.Copy(filePath, backup);
            this.Log().Info($"Back file created at {backup}");

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
        foreach (var local in localMasters)
            if (_masters.SingleOrDefault(x => x.BaseId == local.BaseId) is { } toCompare &&
                toCompare.UniqueId != local.UniqueId)
                return true;

        return false;
    }


    // <inheritdoc />
    public List<VisioMaster> GetOutdatedMasters(IVDocument document)
    {
        if (!_masters.Any()) _masters = _apiFactory.Api.GetCurrentSnapshot().GetAwaiter().GetResult();

        // get the masters of the document
        var needUpdate = new List<VisioMaster>();
        foreach (var master in document.Masters.OfType<IVMaster>())
        {
            var snapshot = _masters.SingleOrDefault(x => x.BaseId == master.BaseID);
            if (snapshot != null && master.UniqueID != snapshot.UniqueId)
                needUpdate.Add(new VisioMaster(master.BaseID, master.Name, master.UniqueID));
        }

        return needUpdate;
    }

    public override void Dispose()
    {
        _timer.Dispose();

        base.Dispose();
    }

    // todo：这段写的不好
    private void UpdateMasterCaches(object state)
    {
        Task.Run(async () =>
        {
            try
            {
                _masters = await _apiFactory.Api.GetCurrentSnapshot();
                _initialized.OnNext(true);

                this.Log().Info($"{_masters.Count()} master snapshots cached.");
            }
            catch (ApiException e)
            {
                this.Log().Error(new NetworkNotValidException());

                _masters = [];
            }
            catch (HttpRequestException e)
            {
                this.Log().Error(new NetworkNotValidException());

                _masters = [];
            }
        });
    }
}