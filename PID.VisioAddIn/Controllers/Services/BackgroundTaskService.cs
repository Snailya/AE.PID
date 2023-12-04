using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reactive.Threading.Tasks;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;
using AE.PID.Interfaces;
using AE.PID.Models;
using AE.PID.Models.Exceptions;
using Microsoft.Office.Interop.Visio;
using NLog;
using PID.Core.Dtos;
using PID.VisioAddIn.Properties;
using Path = System.IO.Path;

namespace AE.PID.Controllers.Services;

public class BackgroundTaskService(ILogger logger, Configuration configuration, HttpClient client)
    : IBackgroundTaskService, IDisposable
{
    private Subject<IVDocument> _manuallyUpdateDocumentMastersTrigger;
    private List<IDisposable> _runningPipelines = [];

    public IObservable<Unit> UpdateAppObservable()
    {
        return CheckForAppUpdateAsync().ToObservable()
            .Where(result => result.IsUpdateAvailable)
            .Do(result => logger.Info($"Found new app version. You can download manually at {result.DownloadUrl}."))
            .SelectMany(result => CacheAppInstaller(result.DownloadUrl),
                (result, installerPath) => new { result.ReleaseNotes, FilePath = installerPath })
            .Select(data =>
            {
                var description = "已为您下载新的PID插件版本。更新内容：" + Environment.NewLine + data.ReleaseNotes +
                                  Environment.NewLine + "请关闭Visio后安装更新";
                return new { DialogResult = AskForUpdate(description), InstallerPath = data.FilePath };
            })
            .Where(result => result.DialogResult == DialogResult.Yes)
            .Do(_ => logger.Info("Trying to update app..."))
            .SelectMany(result =>
            {
                // todo：possible other approach to return Unit
                UpdateApp(result.InstallerPath);
                return Observable.Empty<Unit>();
            })
            .Catch<Unit, Exception>(ex =>
            {
                logger.Error($"Error occured when updating app. '{ex.GetType().Name}: {ex.Message}'");
                return Observable.Empty<Unit>();
            })
            .Finally(() => { logger.Info("App updating service is terminated."); });
    }

    public IObservable<Unit> UpdateLibrariesObservable()
    {
        return Observable.Interval(configuration.LibraryConfiguration.CheckInterval)
            .StartWith(-1) // trigger a update immediately after subscription then invoke by interval
            .SkipWhile(_ => DateTime.Now <
                            configuration.LibraryConfiguration
                                .NextTime) // ignore if the not reached the NextTime in configuration
            .SelectMany(_ => GetLibrariesAsync().ToObservable()) // get library list from server
            .Do(libraries =>
                {
                    configuration.LibraryConfiguration.NextTime =
                        DateTime.Now + configuration.LibraryConfiguration.CheckInterval;
                    logger.Info(
                        $"Found {libraries.Count()} Libraries on server: {string.Join("; ", libraries.Select(x => $"{x.Name}({x.Version})"))}. Next time checking is {configuration.LibraryConfiguration.NextTime}");
                }
            )
            .SelectMany(libraries => libraries)
            .SelectMany(UpdateLibraryAsync, (_, updatedLibrary) => updatedLibrary)
            .Where(updatedLibrary => updatedLibrary != null)
            .Do(updatedLibrary =>
                logger.Info($"Successfully updated {updatedLibrary.Name} to {updatedLibrary.Version}."))
            .Select(_ => Unit.Default)
            .Catch<Unit, Exception>(ex =>
            {
                logger.Error($"Error occured when updating libraries. '{ex.GetType().Name}: {ex.Message}'");
                return Observable.Empty<Unit>();
            })
            .Finally(() =>
            {
                Configuration.Save(configuration);
                logger.Info("Libraries updating service is terminated.");
            });
    }

    public IObservable<Unit> UpdateDocumentMastersObservable()
    {
        _manuallyUpdateDocumentMastersTrigger = new Subject<IVDocument>();

        return Observable.FromEvent<EApplication_DocumentOpenedEventHandler, Document>(
                handler => Globals.ThisAddIn.Application.DocumentOpened += handler,
                handler => Globals.ThisAddIn.Application.DocumentOpened -= handler)
            .Where(document => document.Type == VisDocumentTypes.visTypeDrawing)
            .Merge(_manuallyUpdateDocumentMastersTrigger.Throttle(TimeSpan.FromMilliseconds(300)))
            .Do(document => logger.Info($"Trying to update masters in {document.Name}..."))
            .Select(document => new { Document = document, Mappings = GetMastersNeedUpdate(document) })
            .Do(data => logger.Info(
                $"Found {data.Mappings.Count()} masters in {data.Document.Name} that match items in libraries. Detailed check will performed to keep these into latested version from library."))
            //.ObserveOnDispatcher() // marshalling the rest o the processing to UI thread
            .SelectMany(
                data => UpdateMastersAsync(data.Document, data.Mappings).ToObservable(NewThreadScheduler.Default),
                (_, _) => Unit.Default)
            .Catch<Unit, Exception>(ex =>
            {
                logger.Error($"Error occured when updating libraries. '{ex.GetType().Name}: {ex.Message}'");
                return Observable.Empty<Unit>();
            })
            .Finally(() => { logger.Info("Document masters updating service is terminated."); });
        ;
    }

    public void InvokeUpdateDocumentMasters(IVDocument document)
    {
        _manuallyUpdateDocumentMastersTrigger.OnNext(document);
    }

    public void Dispose()
    {
        _manuallyUpdateDocumentMastersTrigger?.Dispose();
        client?.Dispose();
    }

    private async Task UpdateMastersAsync(IVDocument document, IEnumerable<MasterDocumentLibraryMapping> mappings)
    {
        var undoScope = Globals.ThisAddIn.Application.BeginUndoScope(nameof(UpdateMastersAsync));

        Globals.ThisAddIn.Application.ShowChanges = false;
        var tasks = mappings.Select(item => ReplaceMasterAsync(document, item.BaseId, item.LibraryPath));
        await Task.WhenAll(tasks);
        Globals.ThisAddIn.Application.ShowChanges = true;

        Globals.ThisAddIn.Application.EndUndoScope(undoScope, true);
    }

    private async Task<AppCheckVersionResult> CheckForAppUpdateAsync()
    {
        var local = configuration.Version;

        using var response = await client.GetAsync(configuration.Api + $"/check-version?version={local}");
        response.EnsureSuccessStatusCode();
        var responseBody = await response.Content.ReadAsStringAsync();

        var jsonDocument = JsonDocument.Parse(responseBody);
        var root = jsonDocument.RootElement;

        var isUpdate = root.GetProperty("isUpdateAvailable").GetBoolean();
        if (isUpdate)
            return new AppCheckVersionResult
            {
                IsUpdateAvailable = true,
                DownloadUrl = root.GetProperty("latestVersion").GetProperty("downloadUrl").GetString(),
                ReleaseNotes = root.GetProperty("latestVersion").GetProperty("releaseNotes").GetString()
            };

        return new AppCheckVersionResult { IsUpdateAvailable = false };
    }

    private async Task<string> CacheAppInstaller(string downloadUrl)
    {
        using var response = await client.GetAsync(downloadUrl);
        response.EnsureSuccessStatusCode();

        // Get the content as a stream
        using var contentStream = await response.Content.ReadAsStreamAsync();

        // Save the stream content to a file
        var installerFolder = Path.Combine(Globals.ThisAddIn.DataFolder, "TEMP");
        if (!Directory.Exists(installerFolder)) Directory.CreateDirectory(installerFolder);

        var filePath = Path.Combine(installerFolder,
            Path.GetFileName(
                GetFilenameFromContentDisposition(response.Content.Headers.ContentDisposition.ToString())));
        using var fileStream = File.Create(filePath);
        await contentStream.CopyToAsync(fileStream);

        return Path.GetFullPath(filePath);
    }

    private void UpdateApp(string installerPath)
    {
        logger.Info($"Open the explorer and select {installerPath}");
        Process.Start("explorer.exe", $"/select, \"{installerPath}\"");
    }

    private Task<IEnumerable<LibraryDto>> GetLibrariesAsync()
    {
        return client.GetFromJsonAsync<IEnumerable<LibraryDto>>(configuration.Api + "/libraries");
    }

    private DialogResult AskForUpdate(string description)
    {
        return MessageBox.Show(description, Resources.Product_name, MessageBoxButtons.YesNo);
    }

    private async Task<Library> UpdateLibraryAsync(LibraryDto libraryVersionInfo)
    {
        var local = configuration.LibraryConfiguration.Libraries.SingleOrDefault(x => x.Id == libraryVersionInfo.Id);
        if (local != null && local.Version == libraryVersionInfo.Version) return null;

        local ??= new Library
        {
            Id = libraryVersionInfo.Id,
            Items = libraryVersionInfo.Items.Select(x => new LibraryItem
                { BaseId = x.BaseId, Name = x.Name, UniqueId = x.UniqueId }),
            Name = libraryVersionInfo.Name,
            Version = libraryVersionInfo.Version,
            Path = Path.GetFullPath(Path.ChangeExtension(
                Path.Combine(Globals.ThisAddIn.DataFolder, "Libraries", libraryVersionInfo.Name),
                "vssx"))
        };

        using var response = await client.GetAsync(libraryVersionInfo.DownloadUrl);
        using var contentStream = await response.Content.ReadAsStreamAsync();
        using var fileStream = File.Open(local.Path, FileMode.Create, FileAccess.Write);
        await contentStream.CopyToAsync(fileStream);

        if (configuration.LibraryConfiguration.Libraries.All(x => x.Id != local.Id))
        {
            configuration.LibraryConfiguration.Libraries.Add(local);
        }
        else
        {
            local.Name = libraryVersionInfo.Name;
            local.Version = libraryVersionInfo.Version;
            local.Items = libraryVersionInfo.Items.Select(x => new LibraryItem
                { BaseId = x.BaseId, Name = x.Name, UniqueId = x.UniqueId });
        }

        return local;
    }

    private IEnumerable<MasterDocumentLibraryMapping> GetMastersNeedUpdate(IVDocument document)
    {
        var mappings = new List<MasterDocumentLibraryMapping>();

        foreach (var source in document.Masters.OfType<IVMaster>().ToList())
            if (configuration.LibraryConfiguration.GetItems().SingleOrDefault(x => x.BaseId == source.BaseID) is
                    { } item &&
                item.UniqueId != source.UniqueID)
                mappings.Add(new MasterDocumentLibraryMapping
                {
                    BaseId = source.BaseID,
                    LibraryPath =
                        configuration.LibraryConfiguration.Libraries.SingleOrDefault(x =>
                                x.Items.Any(i => i.BaseId == item.BaseId))!
                            .Path
                });

        return mappings;
    }

    private async Task ReplaceMasterAsync(IVDocument document, string baseId, string targetFilePath)
    {
        // todo: skip one-dimensional shape, or copy the geometry

        // get the origin master from the document stencil
        var source = document.Masters[$"B{baseId}"] ??
                     throw new MasterNotFoundException(baseId);

        if (source.Shapes[1].OneD == (int)VBABool.True)
        {
            logger.Debug(
                $"REPLACEMENT SKIPPED FOR 1D [DOCUMENT: {document.Name}] [LIRARYPATH: {targetFilePath}] [NAME: {source.Name}] [BASEID: {baseId}]");
            return;
        }

        // open the targetFile if not opened
        if (Globals.ThisAddIn.Application.Documents.OfType<IVDocument>().Any(x => x.Path != targetFilePath))
            Globals.ThisAddIn.Application.Documents.OpenEx(targetFilePath, (short)VisOpenSaveArgs.visOpenDocked);

        var target =
            Globals.ThisAddIn.Application.Documents.OfType<IVDocument>().Single(x => x.FullName == targetFilePath)
                .Masters[$"B{baseId}"] ?? throw new MasterNotFoundException(baseId, targetFilePath);

        // get the instances in the active document, convert to list as the master will clear after the delete
        var instances = document.Pages.OfType<IVPage>()
            .SelectMany(x => x.Shapes.OfType<IVShape>()).Where(x => x.Master?.BaseID == baseId).ToList();
        if (instances.Count == 0) return;

        logger.Debug(
            $"REPLACEMENT [DOCUMENT: {document.Name}] [LIRARYPATH: {targetFilePath}] [NAME: {target.Name}] [BASEID: {baseId}] [UNIQUEID: {source.UniqueID} ===> {target.UniqueID}] [COUNT: {instances.Count}]");

        // delete the origin master
        source.Delete();

        //replace with new target one
        var tasks = instances.Select(i => Task.Run(() => i.ReplaceShape(target)));
        await Task.WhenAll(tasks);
        logger.Debug($"REPLACEMENT DONE [NAME: {target.Name}]");
    }

    private static string GetFilenameFromContentDisposition(string contentDisposition)
    {
        string filename = null;

        // Split the header value by semicolons
        var parts = contentDisposition.Split(';');

        // Find the part that starts with "filename="
        foreach (var part in parts)
            if (part.Trim().StartsWith("filename="))
            {
                // Extract the filename
                filename = part.Substring(part.IndexOf('=') + 1).Trim(' ', '"');
                break;
            }

        return filename;
    }

    /// <summary>
    ///     Start the background job, used as a service.
    /// </summary>
    public void Start()
    {
        // starts the background job
        _runningPipelines.Add(UpdateAppObservable().Subscribe());
        _runningPipelines.Add(UpdateLibrariesObservable().Subscribe());
        _runningPipelines.Add(UpdateDocumentMastersObservable().Subscribe());
    }

    /// <summary>
    ///     Terminate the background job.
    /// </summary>
    public void Stop()
    {
        // stops the background job
        _runningPipelines.ForEach(x => x.Dispose());
        _runningPipelines = null;
    }
}