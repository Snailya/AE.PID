using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using AE.PID.Controllers;
using AE.PID.Models;
using AE.PID.Tools;
using AE.PID.Views;
using Microsoft.Office.Interop.Visio;
using Microsoft.Office.Tools.Ribbon;
using NLog;

namespace AE.PID;

public partial class Ribbon
{
    private Configuration _config;
    private Logger _logger;

    private void Ribbon_Load(object sender, RibbonUIEventArgs e)
    {
        _logger = LogManager.GetCurrentClassLogger();
        _config = Globals.ThisAddIn.GetCurrentConfiguration();
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
        Globals.ThisAddIn.ExportService.SaveAsBom();
    }

    private void btnSelectTool_Click(object sender, RibbonControlEventArgs e)
    {
        Globals.ThisAddIn.SelectService.DisplayView();
    }

    private void btnToolbox_Click(object sender, RibbonControlEventArgs e)
    {
        AnchorBarsUsage.ShowAnchorBar(Globals.ThisAddIn.Application);
    }

    private void btnOpenLibraries_Click(object sender, RibbonControlEventArgs e)
    {
        try
        {
            foreach (var path in _config.LibraryConfiguration.Libraries.Select(x => x.Path))
                Globals.ThisAddIn.Application.Documents.OpenEx(path,
                    (short)VisOpenSaveArgs.visOpenDocked);

            _logger.Info($"Opened {_config.LibraryConfiguration.Libraries.Count} libraries.");
        }
        catch (Exception ex)
        {
            _logger.LogUsefulException(ex);
        }
    }

    private void btnUpdateDocumentMasters_Click(object sender, RibbonControlEventArgs e)
    {
        Globals.ThisAddIn.Service.InvokeUpdateDocumentMasters(Globals.ThisAddIn.Application.ActiveDocument);
    }

    private void btnTest(object sender, RibbonControlEventArgs e)
    {
        UpdateShapesLegacy();
    }

    private void btnSettings_Click(object sender, RibbonControlEventArgs e)
    {
        _logger.Info($"Open the explorer and select setting file at {Configuration.GetConfigurationPath()}");
        Process.Start("explorer.exe", $"/select, \"{Configuration.GetConfigurationPath()}\"");
    }
}