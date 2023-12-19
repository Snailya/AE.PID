using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Linq;
using AE.PID.Controllers;
using AE.PID.Controllers.Services;
using AE.PID.Tools;
using Microsoft.Office.Interop.Visio;
using Microsoft.Office.Tools.Ribbon;
using NLog;
using Configuration = AE.PID.Models.Configuration;

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

            var total = toUpdate.SelectMany(x => x.Instances).Count();

            Globals.ThisAddIn.Application.ShowChanges = false;

            foreach (var item in toUpdate)
            {
                item.Old.Delete();

                foreach (var instance in item.Instances) instance.ReplaceShape(item.New);
            }

            Globals.ThisAddIn.Application.ShowChanges = true;
        }, "update-shapes");
    }

    private void btnFlatten_Click(object sender, RibbonControlEventArgs e)
    {
        // todo: refactor using OpenXML
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
                var firstComponent = (short)VisSectionIndices.visSectionFirstComponent;
                var lastComponent = (short)(firstComponent + shape.GeometryCount - 1);

                for (var s = lastComponent; s >= firstComponent; s--)
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

    private void btnSettings_Click(object sender, RibbonControlEventArgs e)
    {
        //_logger.Info($"Open the explorer and select setting file at {Configuration.GetConfigurationPath()}");
        //Process.Start("explorer.exe", $"/select, \"{Configuration.GetConfigurationPath()}\"");
        ConfigurationUpdater.Invoke();
    }

    private void btnTest_Click(object sender, RibbonControlEventArgs e)
    {
        ConfigurationUpdater.Invoke();
    }
}