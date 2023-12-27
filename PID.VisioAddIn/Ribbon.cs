using System;
using System.Linq;
using AE.PID.Controllers;
using AE.PID.Controllers.Services;
using AE.PID.Tools;
using Microsoft.Office.Interop.Visio;
using Microsoft.Office.Tools.Ribbon;
using NLog;

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

    private void btnSelectTool_Click(object sender, RibbonControlEventArgs e)
    {
        ShapeSelector.Invoke(Globals.ThisAddIn.Application.ActivePage);
    }

    private void btnUpdateDocumentMasters_Click(object sender, RibbonControlEventArgs e)
    {
        DocumentUpdater.Invoke(Globals.ThisAddIn.Application.ActiveDocument);
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
        DocumentExporter.Invoke(Globals.ThisAddIn.Application.ActivePage);
    }


    private void btnToolbox_Click(object sender, RibbonControlEventArgs e)
    {
        AnchorBarsUsage.ShowAnchorBar(Globals.ThisAddIn.Application);
    }

    private void btnSettings_Click(object sender, RibbonControlEventArgs e)
    {
        ConfigurationUpdater.Invoke();
    }

    private void btnInitialize_Click(object sender, RibbonControlEventArgs e)
    {
        DocumentInitializer.Invoke(Globals.ThisAddIn.Application.ActiveDocument);
    }
}