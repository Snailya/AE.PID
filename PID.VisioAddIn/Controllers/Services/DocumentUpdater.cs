using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Packaging;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml.Linq;
using AE.PID.Models;
using AE.PID.Models.Exceptions;
using AE.PID.Tools;
using Microsoft.Office.Interop.Visio;
using NLog;
using Path = System.IO.Path;

namespace AE.PID.Controllers.Services;

/// <summary>
/// Compare document stencil with library ones and do updates for the document to keep stencil in time.
/// </summary>
public abstract class DocumentUpdater
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
    private static Subject<IVDocument> ManuallyInvokeTrigger { get; } = new();

    /// <summary>
    ///     Emit a value manually
    /// </summary>
    /// <param name="document"></param>
    public static void Invoke(IVDocument document)
    {
        ManuallyInvokeTrigger.OnNext(document);
    }

    /// <summary>
    /// Listen to both document open event and user click event to monitor if a document master update is needed.
    /// The update process is done on a background thread using OpenXML, so it is extremely fast.
    /// However, a progress bar still provided in case a long time run needed in the future.
    /// </summary>
    public static IDisposable Listen()
    {
        Logger.Info($"Document Update Service started.");

        return Observable
            .FromEvent<EApplication_DocumentOpenedEventHandler, Document>(
                handler => Globals.ThisAddIn.Application.DocumentOpened += handler,
                handler => Globals.ThisAddIn.Application.DocumentOpened -= handler)
            .Where(document => document.Type == VisDocumentTypes.visTypeDrawing)
            .Do(_ => Logger.Info($"Document Update started. {{Initiated by: Document Open Event}}"))
            // manually invoke from ribbon
            .Merge(
                ManuallyInvokeTrigger
                    .Throttle(TimeSpan.FromMilliseconds(300))
                    .Do(_ => Logger.Info($"Document Update started. {{Initiated by: User}}"))
            ).Subscribe(
                document =>
                {
                    Observable.Return(document)
                        .SubscribeOn(TaskPoolScheduler.Default)
                        .SelectMany(data => Task.Run(() => GetUpdatesAsync(data)),
                            (data, mappings) => new { Document = data, Mappings = mappings })
                        .Where(data => data.Mappings is not null && data.Mappings.Any())
                        // prompt user decision
                        .Select(result => new
                            { Info = result, DialogResult = ThisAddIn.AskForUpdate("检测到文档模具与库中模具不一致，是否立即更新文档模具？") })
                        .Where(x => x.DialogResult == DialogResult.Yes)
                        .ObserveOn(Globals.ThisAddIn.SynchronizationContext)
                        // close all document stencils to avoid occupied
                        .Select(x => new { FilePath = Preprocessing(x.Info.Document), x.Info.Mappings })
                        // display a progress bar to do time-consuming operation
                        .Select(data =>
                        {
                            Globals.ThisAddIn.ShowProgressWhileActing(
                                (progress, token) =>
                                {
                                    DoUpdatesByOpenXml(data.FilePath, data.Mappings, progress, token);
                                });
                            return data.FilePath;
                        })
                        .Subscribe(
                            PostProcess,
                            ex => { ThisAddIn.Alert(ex.Message); }
                        );
                },
                ex => { Logger.Error(ex, $"Document Update Service ternimated accidently."); },
                () => { Logger.Error("Document Update Service should never complete."); });
    }

    /// <summary>
    ///     Compare the document stencil with library stuffs.
    /// </summary>
    /// <param name="document"></param>
    /// <returns></returns>
    private static IEnumerable<MasterDocumentLibraryMapping> GetUpdatesAsync(IVDocument document)
    {
        var configuration = Globals.ThisAddIn.Configuration;

        var mappings = new List<MasterDocumentLibraryMapping>();

        foreach (var source in document.Masters.OfType<IVMaster>().ToList())
            if (configuration.LibraryConfiguration.GetItems()
                    .SingleOrDefault(x => x.BaseId == source.BaseID) is { } item
                && item.UniqueId != source.UniqueID)
                mappings.Add(new MasterDocumentLibraryMapping
                {
                    BaseId = source.BaseID,
                    LibraryPath =
                        configuration.LibraryConfiguration.Libraries
                            .SingleOrDefault(x => x.Items.Any(i => i.BaseId == item.BaseId))!
                            .Path,
                    Name = source.Name // this property is not used, it only provide debug information
                });

        return mappings;
    }

    /// <summary>
    ///     Update a document's document stencil by overwrite the masters.xml and related master{i}.xml file in background
    /// </summary>
    /// <param name="filePath"></param>
    /// <param name="mappings"></param>
    /// <param name="progress"></param>
    /// <param name="token"></param>
    /// <exception cref="OperationCanceledException"></exception>
    private static void DoUpdatesByOpenXml(string filePath, IEnumerable<MasterDocumentLibraryMapping> mappings,
        IProgress<int> progress, CancellationToken token)
    {
        var packageMappings = new Dictionary<string, PackageMapping>();

        // todo: consider to serialize the stencil info and offer it from server so that no close of stencils is needed
        // group the library path to reduce io
        foreach (var group in mappings.GroupBy(x => x.LibraryPath))
            // open the target package
            try
            {
                using (var targetPackage = Package.Open(group.Key, FileMode.Open, FileAccess.Read))
                {
                    var styleSheetsInTarget = targetPackage.GetPart(XmlHelper.DocumentPartUri);
                    var stylesInTarget = XmlHelper
                        .GetXElementsByName(XmlHelper.GetXmlFromPart(styleSheetsInTarget), "StyleSheet").Select(x =>
                            new StyleNameId(x.Attribute("NameU")!.Value, x.Attribute("ID")!.Value)).ToList();

                    var mastersPartInTarget = targetPackage.GetPart(XmlHelper.MastersPartUri);
                    var mastersXmlInTarget = XmlHelper.GetXmlFromPart(mastersPartInTarget);

                    // loop the masters element to get the master element, the Rel element is used to get the relationship id from masters.xml to master{i}.xml
                    foreach (var masterElement in XmlHelper.GetXElementsByName(mastersXmlInTarget, "Master"))
                    {
                        if (token.IsCancellationRequested)
                        {
                            Logger.Info("User cancelled the update process.");
                            throw new OperationCanceledException(token);
                        }

                        // get the baseID
                        var baseId = masterElement.Attribute("BaseID")!.Value;

                        // get the rel:id in order to get related MasterPart
                        var relElement = masterElement.Descendants(XmlHelper.MainNs + "Rel").First();
                        var relId = relElement.Attribute(XmlHelper.RelNs + "id")!.Value;
                        var rel = mastersPartInTarget.GetRelationship(relId);
                        var masterPart =
                            targetPackage.GetPart(PackUriHelper.ResolvePartUri(rel.SourceUri, rel.TargetUri));
                        var mapping = new PackageMapping(masterElement, XmlHelper.GetXmlFromPart(masterPart),
                            stylesInTarget);
                        packageMappings.Add(baseId, mapping);
                    }
                }
            }
            catch (IOException ioException)
            {
                Logger.Error(ioException,
                    $"Unable to open the library as is currently used by another process. Please close the stencil document in source file and retry.");
                throw;
            }

        using (var sourcePackage = Package.Open(filePath, FileMode.Open, FileAccess.ReadWrite))
        {
            // First update the styles for master.xml as the style id name match may vary from documents.
            // For example, a style named MyStyle in the stencil document might have an id of 10, but a id of 11 in drawing document.
            // If the administrator changed the style of any master in stencil, replace the MasterContents in drawing document with that in stencil might mislead to a wrong style.
            var styleSheetsInSource = sourcePackage.GetPart(XmlHelper.DocumentPartUri);
            var stylesInSource = XmlHelper
                .GetXElementsByName(XmlHelper.GetXmlFromPart(styleSheetsInSource), "StyleSheet").Select(x =>
                    new StyleNameId(x.Attribute("NameU")!.Value, x.Attribute("ID")!.Value)).ToList();

            foreach (var packageMapping in packageMappings)
            {
                if (token.IsCancellationRequested)
                {
                    Logger.Info("User cancelled the update process.");
                    throw new OperationCanceledException(token);
                }

                var shapeElements = packageMapping.Value.MasterXml.Descendants(XmlHelper.MainNs + "Shape");
                foreach (var shapeElement in shapeElements)
                {
                    var lineAttribute = shapeElement.Attribute("LineStyle");
                    if (lineAttribute == null) continue;

                    var lineStyleNameInTarget = packageMapping.Value.StyleNameIds
                        .Single(x => x.Id == lineAttribute.Value).Name;
                    var lineStyleIdInSource = stylesInSource.SingleOrDefault(x => x.Name == lineStyleNameInTarget)?.Id;
                    if (lineStyleIdInSource != null) lineAttribute.Value = lineStyleIdInSource;

                    var fillAttribute = shapeElement.Attribute("FillStyle");
                    if (fillAttribute == null) continue;

                    var fillStyleNameInTarget = packageMapping.Value.StyleNameIds
                        .Single(x => x.Id == fillAttribute.Value).Name;
                    var fillStyleIdInSource = stylesInSource.SingleOrDefault(x => x.Name == fillStyleNameInTarget)?.Id;
                    if (fillStyleIdInSource != null) fillAttribute.Value = fillStyleIdInSource;

                    var textAttribute = shapeElement.Attribute("TextStyle");
                    if (textAttribute == null) continue;

                    var textStyleNameInTarget = packageMapping.Value.StyleNameIds
                        .Single(x => x.Id == textAttribute.Value).Name;
                    var textStyleIdInSource = stylesInSource.SingleOrDefault(x => x.Name == textStyleNameInTarget)?.Id;
                    if (textStyleIdInSource != null) textAttribute.Value = textStyleIdInSource;
                }
            }

            // After revise the style ids in MasterContents, loop through the drawing document to replace the masters.xml and master{i}.xml
            var mastersPartInSource = sourcePackage.GetPart(XmlHelper.MastersPartUri);
            var mastersXmlInSource = XmlHelper.GetXmlFromPart(mastersPartInSource);

            var masterElements = XmlHelper.GetXElementsByName(mastersXmlInSource, "Master").ToList();
            for (var index = 0; index < masterElements.Count; index++)
            {
                progress.Report(index * 100 / masterElements.Count);

                var masterElement = masterElements[index];
                if (token.IsCancellationRequested)
                {
                    Logger.Info("User cancelled the update process.");
                    throw new OperationCanceledException(token);
                }

                // get the map
                var baseId = masterElement.Attribute("BaseID")!.Value;
                if (!packageMappings.TryGetValue(baseId, out var mapping)) continue;

                // get id attribute
                var id = masterElement.Attribute("ID")!.Value;
                mapping.MasterElement.Attribute("ID")!.SetValue(id);

                // get the rel
                var relElement = masterElement.Descendants(XmlHelper.MainNs + "Rel").First();
                mapping.MasterElement.Descendants(XmlHelper.MainNs + "Rel").First().ReplaceWith(relElement);

                // get the related master xml
                var relId = relElement.Attribute(XmlHelper.RelNs + "id")!.Value;
                var rel = mastersPartInSource.GetRelationship(relId);
                var masterPart = sourcePackage.GetPart(PackUriHelper.ResolvePartUri(rel.SourceUri, rel.TargetUri));

                XmlHelper.SaveXDocumentToPart(masterPart, mapping.MasterXml);

                // overwrite the origin masterElement
                masterElement.ReplaceWith(mapping.MasterElement);
            }

            progress.Report(100);

            XmlHelper.RecalculateDocument(sourcePackage);
            XmlHelper.SaveXDocumentToPart(mastersPartInSource, mastersXmlInSource);
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="filePath"></param>
    private static void PostProcess(string filePath)
    {
        Logger.Info(
            $"Document masters updated successfully.");

        try
        {
            // open all stencils 
            foreach (var path in Globals.ThisAddIn.Configuration.LibraryConfiguration.Libraries
                         .Select(x => x.Path))
                Globals.ThisAddIn.Application.Documents.OpenEx(path,
                    (short)VisOpenSaveArgs.visOpenDocked);
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Failed to restore stencils after updated");
        }

        ThisAddIn.Alert("更新成功，请在新文件打开后手动另存。");
        Globals.ThisAddIn.Application.Documents.OpenEx(filePath,
            (short)VisOpenSaveArgs.visOpenRW);
    }

    /// <summary>
    /// Close all opened document in docked window to prevent document busy, copy the source file to a temporary path.
    /// </summary>
    /// <param name="document"></param>
    /// <returns></returns>
    private static string Preprocessing(IVDocument document)
    {
        // todo: initialize style

        foreach (var doc in
                 Globals.ThisAddIn.Application.Documents.OfType<IVDocument>()
                     .Where(doc => doc.Type == VisDocumentTypes.visTypeStencil).ToList())
            doc.Close();

        // create a copy of source file
        var copied = Path.Combine(ThisAddIn.TmpFolder, Path.ChangeExtension(Path.GetRandomFileName(), "vsdx"));
        File.Copy(document.FullName, copied);

        return copied;
    }

    /// <summary>
    ///     Update document stencil by delete the origin stencil from document master, then replace all instances with new one
    ///     by calling Shape.Replace().
    ///     This method may execute in a long time ant might cause the main thread run not as expected. Will block the ui thread, as it also marshalling for the ui thread.
    /// </summary>
    /// <param name="document"></param>
    /// <param name="mappings"></param>
    /// <param name="progress"></param>
    /// <param name="token"></param>
    /// <exception cref="OperationCanceledException"></exception>
    public static void DoUpdatesInSite(IVDocument document, IEnumerable<MasterDocumentLibraryMapping> mappings,
        IProgress<int> progress,
        CancellationToken token)
    {
        Globals.ThisAddIn.Application.ShowChanges = false;
        var undoScope = Globals.ThisAddIn.Application.BeginUndoScope(nameof(DoUpdatesInSite));
        try
        {
            var masterDocumentLibraryMappings = mappings as MasterDocumentLibraryMapping[] ?? mappings.ToArray();
            for (var i = 0; i < masterDocumentLibraryMappings.Length; i++)
            {
                if (token.IsCancellationRequested)
                {
                    Logger.Info("User cancelled the update process.");
                    throw new OperationCanceledException(token);
                }

                var mapping = masterDocumentLibraryMappings[i];
                ReplaceMaster(document, mapping.BaseId, mapping.LibraryPath);

                progress.Report((i + 1) * 100 / masterDocumentLibraryMappings.Length);
            }

            Globals.ThisAddIn.Application.EndUndoScope(undoScope, true);
        }
        catch (Exception)
        {
            Globals.ThisAddIn.Application.EndUndoScope(undoScope, false);
            throw;
        }
        finally
        {
            Globals.ThisAddIn.Application.ShowChanges = true;
        }
    }

    private static void ReplaceMaster(IVDocument document, string baseId, string targetFilePath)
    {
        var logger = LogManager.GetCurrentClassLogger();

        // get the origin master from the document stencil
        var source = document.Masters[$"B{baseId}"] ??
                     throw new MasterNotFoundException(baseId);

        if (source.Shapes[1].OneD == (int)VBABool.True)
        {
            logger.Debug(
                $"REPLACEMENT SKIPPED FOR 1D [DOCUMENT: {document.Name}] [LIRARYPATH: {targetFilePath}] [NAME: {source.Name}] [BASEID: {baseId}]");
            return;
        }

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
        instances.ForEach(i => { i.ReplaceShape(target); });

        logger.Debug($"REPLACEMENT DONE [NAME: {target.Name}]");
    }

    private class PackageMapping(XElement masterElement, XDocument masterXml, IEnumerable<StyleNameId> styleNameIds)
    {
        public XElement MasterElement { get; } = masterElement;
        public XDocument MasterXml { get; } = masterXml;

        public IEnumerable<StyleNameId> StyleNameIds { get; } = styleNameIds;
    }

    private class StyleNameId(string name, string id)
    {
        public string Name { get; } = name;
        public string Id { get; } = id;
    }
}