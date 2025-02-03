using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;
using AE.PID.Client.Core;
using AE.PID.Client.Core.Exceptions;
using AE.PID.Client.Core.VisioExt;
using AE.PID.Client.Core.VisioExt.Models;
using AE.PID.Core.DTOs;
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


    // <inheritdoc />
    public List<VisioMaster> GetOutdatedMasters { get; set; }

    public async Task UpdateAsync(string filePath, string[]? excludes = null)
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
            var filePart = new ByteArrayPart(fileBytes, Path.GetFileName(filePath));

            var result = await _apiFactory.Api.Update(filePart, excludes);

            // create a copy of the source file
            var backup = Path.ChangeExtension(filePath, ".bak");
            if (File.Exists(backup))
                backup = Path.Combine(Path.GetDirectoryName(backup) ?? string.Empty,
                    Path.GetFileNameWithoutExtension(backup) + DateTime.Now.ToString("yyyyMMddhhmmss") + ".bak");
            File.Copy(filePath, backup);

            // overwrite the origin file after a successful update
            using (var fileStream = File.Open(filePath, FileMode.Create, FileAccess.Write))
            {
                await result.CopyToAsync(fileStream);
                fileStream.Close();
            }
        }
        catch (ApiException e) when (e.StatusCode == HttpStatusCode.BadRequest)
        {
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
    public bool HasUpdate(IEnumerable<MasterSnapshotDto> localMasters, string[]? excludes = null)
    {
        foreach (var local in localMasters)
            if (_masters.SingleOrDefault(x => x.BaseId == local.BaseId) is { } toCompare &&
                toCompare.UniqueId != local.UniqueId)
                return excludes == null || !excludes.Contains(local.UniqueId);

        return false;
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