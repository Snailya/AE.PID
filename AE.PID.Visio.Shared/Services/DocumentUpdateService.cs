using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using AE.PID.Core.DTOs;
using AE.PID.Visio.Core.Exceptions;
using AE.PID.Visio.Core.Interfaces;
using Refit;
using Splat;
using Path = System.IO.Path;

namespace AE.PID.Visio.Shared.Services;

public class DocumentUpdateService : DisposableBase, IDocumentUpdateService
{
    private static IEnumerable<MasterSnapshotDto> _masters = [];
    private readonly IApiFactory<IDocumentApi> _apiFactory;
    private readonly Timer _timer;

    public DocumentUpdateService(IApiFactory<IDocumentApi> apiFactory)
    {
        _apiFactory = apiFactory;
        _timer = new Timer(UpdateMasterCaches, null, TimeSpan.Zero, TimeSpan.FromHours(1));
    }

    // <inheritdoc />
    public bool HasUpdate(IEnumerable<MasterSnapshotDto> masters)
    {
        foreach (var master in masters)
            if (_masters.SingleOrDefault(x => x.BaseId == master.BaseId) is { } toCompare &&
                toCompare.UniqueId != master.UniqueId)
                return true;

        return false;
    }

    // <inheritdoc />
    public async Task UpdateAsync(string filePath)
    {
        if (string.IsNullOrEmpty(filePath)) throw new ArgumentNullException(nameof(filePath));
        if (Path.GetExtension(filePath) != ".vsdx")
            throw new ArgumentException(
                $"{filePath} is not a valid document for document update. Only visio drawing file is valid.");
        if (!File.Exists(filePath)) throw new FileNotFoundException($"{filePath} not exist on the local storage.");

        // convert the file to byte-array content and sent as byte-array
        // because there is an encrypted system on end user, so directly transfer the file to server will not be able to read in the server side
        var packageBytes = File.ReadAllBytes(filePath);
        var content = new ByteArrayContent(packageBytes);

        try
        {
            var result = await _apiFactory.Api.Update(content);

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
            throw new DocumentNotRecognizedException(e.Message);
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