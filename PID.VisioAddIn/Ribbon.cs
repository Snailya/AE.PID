using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using AE.PID.Controllers;
using AE.PID.Controllers.Services;
using AE.PID.Models;
using AE.PID.Models.Exceptions;
using AE.PID.Tools;
using AE.PID.Views;
using Microsoft.Office.Interop.Visio;
using Microsoft.Office.Tools.Ribbon;
using NLog;
using MessageBox = System.Windows.Forms.MessageBox;

namespace AE.PID;

public partial class Ribbon
{
    private Logger _logger;


    private void Ribbon_Load(object sender, RibbonUIEventArgs e)
    {
        _logger = LogManager.GetCurrentClassLogger();
    }

    private void Ribbon_Close(object sender, EventArgs e)
    {
    }

    private static void RunScoped(Action action, string actionName = "")
    {
        var undoScope =
            Globals.ThisAddIn.Application.BeginUndoScope(string.IsNullOrEmpty(actionName)
                ? Guid.NewGuid().ToString()
                : actionName);
        action.Invoke();
        Globals.ThisAddIn.Application.EndUndoScope(undoScope, true);
    }

    public static void UpdateShapesLegacy()
    {
        RunScoped(() =>
        {
            // get all master from ae custom library
            var stencils = Globals.ThisAddIn.Application.Documents.OfType<Document>()
                .Where(x => x.Name.StartsWith("AE") && !x.Name.Contains("管线"))
                .SelectMany(x => x.Masters.OfType<Master>());

            // find out instances that need update
            List<(Master Old, Master New, IEnumerable<Shape> Instances)> toUpdate = new();
            foreach (var master in Globals.ThisAddIn.Application.ActiveDocument.Masters.OfType<Master>())
            {
                var newMaster =
                    stencils.SingleOrDefault(x =>
                        x.BaseID == master.BaseID && x.UniqueID != master.UniqueID); // only changed
                if (newMaster == null)
                {
                    Debug.WriteLine("This master has no difference compared with the library, skipped");
                    continue;
                }

                //ThisAddIn.Logger.Information("{Name}({NameU}) need update.", master.Name, master.NameU);
                var instancesToUpdate = Globals.ThisAddIn.Application.ActiveDocument.Pages.OfType<Page>()
                    .SelectMany(x => x.Shapes.OfType<Shape>()).Where(x => x.Master == master).ToList();
                Debug.WriteLine($"There are {instancesToUpdate.Count} {master.Name}(s) need to update");
                toUpdate.Add((master, newMaster, instancesToUpdate));
            }

            var progressBar = new ProgressBar();
            var total = toUpdate.SelectMany(x => x.Instances).Count();
            progressBar.Show();

            Globals.ThisAddIn.Application.ShowChanges = false;

            var index = 0;

            foreach (var item in toUpdate)
            {
                item.Old.Delete();

                foreach (var instance in item.Instances)
                {
                    instance.ReplaceShape(item.New);
                    index++;
                    progressBar.SetValue(100 * index / total);
                }
            }

            progressBar.Close();
            Globals.ThisAddIn.Application.ShowChanges = true;
        }, "update-shapes");
    }


    private void btnFlatten_Click(object sender, RibbonControlEventArgs e)
    {
        RunScoped(() =>
        {
            // delete all document masters
            foreach (var master in Globals.ThisAddIn.Application.ActiveDocument.Masters.OfType<Master>())
                master.Delete();

            // ungroup all shapes
            var shapesToFlatten = Globals.ThisAddIn.Application.ActiveDocument.Pages.OfType<Page>()
                .SelectMany(x => x.Shapes.OfType<Shape>()).ToList();
            foreach (var shape in shapesToFlatten)
                shape.Ungroup();

            // delete the invisible shapes
            foreach (var shape in Globals.ThisAddIn.Application.ActiveDocument.Pages.OfType<Page>()
                         .SelectMany(x => x.Shapes.OfType<Shape>()))
            {
                // ignore the performance, if too slow consider check if there's no visible geometry, delete shape directly
                var firstCompoent = (short)VisSectionIndices.visSectionFirstComponent;
                var lastCompoent = (short)(firstCompoent + shape.GeometryCount - 1);

                for (var s = lastCompoent; s >= firstCompoent; s--)
                    if (shape.CellsSRC[s, 0, (short)VisCellIndices.visCompNoShow].FormulaU == "TRUE")
                        shape.DeleteSection(s);

                if (shape.GeometryCount == 0 && string.IsNullOrEmpty(shape.Text)) shape.Delete();
            }
        }, ((RibbonButton)sender).Label);
    }

    private void btnExport_Click(object sender, RibbonControlEventArgs e)
    {
        Exporter.Invoke(Globals.ThisAddIn.Application.ActivePage);
    }

    private void btnSelectTool_Click(object sender, RibbonControlEventArgs e)
    {
        Selector.Invoke(Globals.ThisAddIn.Application.ActivePage);
    }

    private void btnToolbox_Click(object sender, RibbonControlEventArgs e)
    {
        AnchorBarsUsage.ShowAnchorBar(Globals.ThisAddIn.Application);
    }

    private void btnOpenLibraries_Click(object sender, RibbonControlEventArgs e)
    {
        try
        {
            foreach (var path in Globals.ThisAddIn.Configuration.LibraryConfiguration.Libraries.Select(x => x.Path))
                Globals.ThisAddIn.Application.Documents.OpenEx(path,
                    (short)VisOpenSaveArgs.visOpenDocked);

            _logger.Info($"Opened {Globals.ThisAddIn.Configuration.LibraryConfiguration.Libraries.Count} libraries.");
        }
        catch (Exception ex)
        {
            _logger.LogUsefulException(ex);
        }
    }

    private void btnUpdateDocumentMasters_Click(object sender, RibbonControlEventArgs e)
    {
        DocumentUpdater.Invoke(Globals.ThisAddIn.Application.ActiveDocument);
    }

    private void ReplaceMaster(IVDocument document, string baseId, string targetFilePath)
    {
        // get the origin master from the document stencil
        var source = document.Masters[$"B{baseId}"] ??
                     throw new MasterNotFoundException(baseId);

        if (source.Shapes[1].OneD == (int)VBABool.True)
        {
            _logger.Debug(
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

        _logger.Debug(
            $"REPLACEMENT [DOCUMENT: {document.Name}] [LIRARYPATH: {targetFilePath}] [NAME: {target.Name}] [BASEID: {baseId}] [UNIQUEID: {source.UniqueID} ===> {target.UniqueID}] [COUNT: {instances.Count}]");

        // delete the origin master
        source.Delete();

        //replace with new target one
        instances.ForEach(i => i.ReplaceShape(target));
        _logger.Debug($"REPLACEMENT DONE [NAME: {target.Name}]");
    }

    private void btnTest(object sender, RibbonControlEventArgs e)
    {
    }

    private void btnTest2(object sender, RibbonControlEventArgs e)
    {
    }


    private void btnTest3(object sender, RibbonControlEventArgs e)
    {
        // get the origin master from the document stencil
        var document = Globals.ThisAddIn.Application.ActiveDocument;
        var baseId = "{DAA3F7AE-0468-4270-96D9-9A5ACD0CB612}";
        var targetFilePath = "C:\\Users\\lijin\\AppData\\Roaming\\AE\\PID\\Libraries\\AE基础.vssx";


        var source = document.Masters["B{DAA3F7AE-0468-4270-96D9-9A5ACD0CB612}"] ??
                     throw new MasterNotFoundException(baseId);
        // open the targetFile if not opened
        if (Globals.ThisAddIn.Application.Documents.OfType<IVDocument>().Any(x => x.Path != targetFilePath))
            Globals.ThisAddIn.Application.Documents.OpenEx(targetFilePath, (short)VisOpenSaveArgs.visOpenDocked);

        var target =
            Globals.ThisAddIn.Application.Documents.OfType<IVDocument>().Single(x => x.FullName == targetFilePath)
                .Masters[$"B{baseId}"] ?? throw new MasterNotFoundException(baseId, targetFilePath);

        if (source.Shapes[1].OneD == (int)VBABool.True) return;

        var stopwatch = new Stopwatch();
        stopwatch.Start();

        var undoScope2 = Globals.ThisAddIn.Application.BeginUndoScope(nameof(btnTest));
        Globals.ThisAddIn.Application.ShowChanges = false;

        //// get the instances in the active document, convert to list as the master will clear after the delete
        //var instances2 = document.Pages.OfType<IVPage>()
        //    .SelectMany(x => x.Shapes.OfType<IVShape>()).Where(x => x.Master?.BaseID == baseId).ToList();
        //if (instances2.Count == 0) return;

        // delete the origin master


        Globals.ThisAddIn.Application.ShowChanges = true;
        Globals.ThisAddIn.Application.EndUndoScope(undoScope2, false);
        var time2 = stopwatch.Elapsed;
        stopwatch.Stop();
        MessageBox.Show(stopwatch.Elapsed.ToString());
    }

    private void btnSettings_Click(object sender, RibbonControlEventArgs e)
    {
        _logger.Info($"Open the explorer and select setting file at {Configuration.GetConfigurationPath()}");
        Process.Start("explorer.exe", $"/select, \"{Configuration.GetConfigurationPath()}\"");
    }
}