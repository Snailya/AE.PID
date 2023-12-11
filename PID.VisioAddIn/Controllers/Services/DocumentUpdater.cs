using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Subjects;
using System.Threading;
using AE.PID.Models;
using AE.PID.Models.Exceptions;
using Microsoft.Office.Interop.Visio;
using NLog;

namespace AE.PID.Controllers.Services;

public abstract class DocumentUpdater
{
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
    ///     Update document stencil by delete the origin stencil from document master, then replace all instances with new one
    ///     by calling Shape.Replace().
    ///     This method may execute in a long time ant might cause the main thread run not as expected.
    /// </summary>
    /// <param name="document"></param>
    /// <param name="mappings"></param>
    /// <param name="token"></param>
    /// <exception cref="OperationCanceledException"></exception>
    public static void DoUpdates(IVDocument document, IEnumerable<MasterDocumentLibraryMapping> mappings,
        CancellationToken token)
    {
        var logger = LogManager.GetCurrentClassLogger();
        var configuration = Globals.ThisAddIn.Configuration;

        Globals.ThisAddIn.Application.ShowChanges = false;
        var undoScope = Globals.ThisAddIn.Application.BeginUndoScope(nameof(DoUpdates));
        try
        {
            foreach (var path in configuration.LibraryConfiguration.Libraries.Select(x => x.Path))
                Globals.ThisAddIn.Application.Documents.OpenEx(path,
                    (short)VisOpenSaveArgs.visOpenDocked);

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
        instances.ForEach(i => i.ReplaceShape(target));
        logger.Debug($"REPLACEMENT DONE [NAME: {target.Name}]");
    }
}