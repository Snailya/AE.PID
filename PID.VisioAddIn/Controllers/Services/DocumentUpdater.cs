using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Packaging;
using System.Linq;
using System.Reactive.Subjects;
using System.Threading;
using System.Xml.Linq;
using AE.PID.Models;
using AE.PID.Models.Exceptions;
using Microsoft.Office.Interop.Visio;
using NLog;

namespace AE.PID.Controllers.Services;

/// <summary>
/// Compare document stencil with library ones and do updates for the document to keep stencil in time.
/// </summary>
public abstract class DocumentUpdater
{
    /// <summary>
    /// Trigger used for ui Button to invoke the update event.
    /// </summary>
    public static Subject<IVDocument> ManuallyInvokeTrigger { get; } = new();

    /// <summary>
    ///     Emit a value manually
    /// </summary>
    /// <param name="document"></param>
    public static void Invoke(IVDocument document)
    {
        ManuallyInvokeTrigger.OnNext(document);
    }

    /// <summary>
    ///     Compare the document stencil with library stuffs.
    /// </summary>
    /// <param name="document"></param>
    /// <returns></returns>
    public static IEnumerable<MasterDocumentLibraryMapping> GetUpdatesAsync(IVDocument document)
    {
        var configuration = Globals.ThisAddIn.Configuration;

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
    
    /// <summary>
    ///     Update a document's document stencil by overwrite the masters.xml and related master{i}.xml file in background
    /// </summary>
    /// <param name="filePath"></param>
    /// <param name="mappings"></param>
    /// <param name="progress"></param>
    /// <param name="token"></param>
    /// <exception cref="OperationCanceledException"></exception>
    public static void DoUpdatesByOpenXml(string filePath, IEnumerable<MasterDocumentLibraryMapping> mappings,
        IProgress<int> progress, CancellationToken token)
    {
        var logger = LogManager.GetCurrentClassLogger();
        var packageMappings = new Dictionary<string, PackageMapping>();

        // group the library path to reduce io
        foreach (var group in mappings.GroupBy(x => x.LibraryPath))
            // open the target package
            using (var targetPackage = Package.Open(group.Key, FileMode.Open, FileAccess.Read))
            {
                var mastersPartInTarget = targetPackage.GetPart(XmlHelper.MastersPartUri);
                var mastersXmlInTarget = XmlHelper.GetXmlFromPart(mastersPartInTarget);

                // loop the masters element to get the master element, the Rel element is used to get the relationship id from masters.xml to master{i}.xml
                foreach (var masterElement in XmlHelper.GetXElementsByName(mastersXmlInTarget, "Master"))
                {
                    // get the baseID
                    var baseId = masterElement.Attribute("BaseID")!.Value;

                    // get the rel:id in order to get related MasterPart
                    var relElement = masterElement.Descendants(XmlHelper.MainNs + "Rel").First();
                    var relId = relElement.Attribute(XmlHelper.RelNs + "id")!.Value;
                    var rel = mastersPartInTarget.GetRelationship(relId);
                    var masterPart = targetPackage.GetPart(PackUriHelper.ResolvePartUri(rel.SourceUri, rel.TargetUri));
                    var mapping = new PackageMapping(baseId, masterElement, XmlHelper.GetXmlFromPart(masterPart));
                    packageMappings.Add(baseId, mapping);
                }
            }

        using (var sourcePackage = Package.Open(filePath, FileMode.Open, FileAccess.ReadWrite))
        {
            var mastersPartInSource = sourcePackage.GetPart(XmlHelper.MastersPartUri);
            var mastersXmlInSource = XmlHelper.GetXmlFromPart(mastersPartInSource);

            // loop the source file
            var masterElements = XmlHelper.GetXElementsByName(mastersXmlInSource, "Master").ToList();
            for (var index = 0; index < masterElements.Count; index++)
            {
                var masterElement = masterElements[index];
                if (token.IsCancellationRequested)
                {
                    logger.Info("User cancelled the update process.");
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

                progress.Report((index + 1) * 100 / masterElements.Count);
            }

            XmlHelper.SaveXDocumentToPart(mastersPartInSource, mastersXmlInSource);
        }
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
        var logger = LogManager.GetCurrentClassLogger();

        Globals.ThisAddIn.Application.ShowChanges = false;
        var undoScope = Globals.ThisAddIn.Application.BeginUndoScope(nameof(DoUpdatesInSite));
        try
        {
            var masterDocumentLibraryMappings = mappings as MasterDocumentLibraryMapping[] ?? mappings.ToArray();
            for (var i = 0; i < masterDocumentLibraryMappings.Length; i++)
            {
                if (token.IsCancellationRequested)
                {
                    logger.Info("User cancelled the update process.");
                    throw new OperationCanceledException(token);
                }

                var mapping = masterDocumentLibraryMappings[i];
                ReplaceMaster(document, mapping.BaseId, mapping.LibraryPath);

                progress.Report((i + 1) * 100 / masterDocumentLibraryMappings.Length);
            }

            Globals.ThisAddIn.Application.EndUndoScope(undoScope, true);
        }
        catch (Exception e)
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
    
    private class PackageMapping(string baseId, XElement masterElement, XDocument masterXml)
    {
        public string BaseId { get; } = baseId;
        public XElement MasterElement { get; } = masterElement;
        public XDocument MasterXml { get; } = masterXml;
    }
}