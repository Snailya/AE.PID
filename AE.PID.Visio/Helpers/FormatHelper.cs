using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using AE.PID.Visio.Exceptions;
using AE.PID.Visio.Extensions;
using Microsoft.Office.Interop.Excel;
using Microsoft.Office.Interop.Visio;
using Splat;
using Font = Microsoft.Office.Interop.Visio.Font;
using Page = Microsoft.Office.Interop.Visio.Page;
using Shape = Microsoft.Office.Interop.Visio.Shape;

namespace AE.PID.Visio.Helpers;

public abstract class FormatHelper
{
    public const string NormalStyleName = "AE Normal";
    private const string PipelineStyleName = "AE PipeLine";

    public static void FormatPage(Page page)
    {
        var document = page.Document;
        var undoScope = page.Application.BeginUndoScope("Format Page");

        try
        {
            // the style is strongly relevant to font 等线， if the font is missing, this step should be skipped.
            if (document.Fonts.OfType<Font>().SingleOrDefault(x => x.Name == "等线") is { } font)
                SetupStyles(document, font);

            SetupLayoutAndRouting(page);
            // the grid setting is not relevant to any other custom object import by this program, so it should never have failed
            // so set it in the first.
            SetupRulerAndGrid(page);

            // insert the frame at the 0,0 potion
            var frame = InsertFrameIfNotExist(page);
            if (frame != null)
                InsertTableIfNotExist(page, frame);

            // set the view to make the frame center in the window
            Globals.ThisAddIn.Application.ActiveWindow.ViewFit = (int)VisWindowFit.visFitPage;

            document.EndUndoScope(undoScope, true);
        }
        catch (Exception ex)
        {
            document.EndUndoScope(undoScope, false);
            LogHost.Default.Error(ex, "Failed to format page.");
        }
    }

    private static void SetupRulerAndGrid(IVPage page)
    {
        page.PageSheet.CellsSRC[(short)VisSectionIndices.visSectionObject, (short)VisRowIndices.visRowRulerGrid,
            (short)VisCellIndices.visXGridDensity].FormulaU = ((int)VisCellVals.visGridFixed).ToString();
        page.PageSheet.CellsSRC[(short)VisSectionIndices.visSectionObject, (short)VisRowIndices.visRowRulerGrid,
            (short)VisCellIndices.visYGridDensity].FormulaU = ((int)VisCellVals.visGridFixed).ToString();
        page.PageSheet.CellsSRC[(short)VisSectionIndices.visSectionObject, (short)VisRowIndices.visRowRulerGrid,
            (short)VisCellIndices.visXGridSpacing].FormulaU = "2.5mm";
        page.PageSheet.CellsSRC[(short)VisSectionIndices.visSectionObject, (short)VisRowIndices.visRowRulerGrid,
            (short)VisCellIndices.visYGridSpacing].FormulaU = "2.5mm";
        page.PageSheet.CellsSRC[(short)VisSectionIndices.visSectionObject, (short)VisRowIndices.visRowRulerGrid,
            (short)VisCellIndices.visXGridOrigin].FormulaU = "0mm";
        page.PageSheet.CellsSRC[(short)VisSectionIndices.visSectionObject, (short)VisRowIndices.visRowRulerGrid,
            (short)VisCellIndices.visYGridOrigin].FormulaU = "0mm";
        
        LogHost.Default.Info($"Grid setup for {page.Name} finished");
    }

    private static void SetupLayoutAndRouting(IVPage page)
    {
        page.PageSheet.CellsSRC[(short)VisSectionIndices.visSectionObject, (short)VisRowIndices.visRowPageLayout,
            (short)VisCellIndices.visPLOJumpFactorX].FormulaU = "1";
        LogHost.Default.Info($"Layout and routing setup for {page.Name} finished");
    }

    private static Shape? InsertFrameIfNotExist(IVPage page)
    {
        const string baseId = "{7811D65E-9633-4E98-9FCD-B496A8B823A7}";

        var frame = page.Shapes.OfType<Shape>().FirstOrDefault(x => x.Master.BaseID == baseId);

        if (frame != null) return frame;

        try
        {
            var frameObject = page.Document.GetMaster(baseId);

            frame = page.DropMetric(frameObject, (0, 0));
            page.AutoSizeDrawing();

            return frame;
        }
        catch (MasterNotValidException)
        {
            MessageBox.Show(@"未能找到图框，请检查AE逻辑.vssx文件。");
        }
        catch (Exception e)
        {
            LogHost.Default.Error(e,
                "Failed to insert frame at origin");
        }

        return null;
    }

    private static (Shape? Table1, Shape? Table2) InsertTableIfNotExist(IVPage page, Shape frame)
    {
        const string table1BaseId = "{D1A49D75-2A8B-4F4B-9A3A-27A0BC63D08D}";
        const string table2BaseId = "{4B7CA2AC-E82E-4382-80F8-D2E0CC85B151}";

        if (page.Shapes.OfType<Shape>().Any(x => x.Master.BaseID == table1BaseId)) return (null, null);
        if (page.Shapes.OfType<Shape>().Any(x => x.Master.BaseID == table2BaseId)) return (null, null);

        try
        {
            var (_, _, right, top) = frame.BoundingBoxMetric((short)VisBoundingBoxArgs.visBBoxDrawingCoords +
                                                             (short)VisBoundingBoxArgs.visBBoxExtents);
            var table1Object = page.Document.GetMaster(table1BaseId);
            var table2Object = page.Document.GetMaster(table2BaseId);

            var table1 = page.DropMetric(table1Object, (right - 130, top - 63));
            var table2 = page.DropMetric(table2Object, (right - 130, top - 229));

            return (table1, table2);
        }
        catch (MasterNotValidException)
        {
            MessageBox.Show(@"未能找到PCI符号说明，请检查AE标识.vssx文件是否已打开。");
        }
        catch (Exception e)
        {
            LogHost.Default.Error(e,
                "Failed to insert frame at origin");
        }

        return (null, null);
    }

    private static void SetupStyles(IVDocument document, Font? font = null)
    {
        // setup or initialize ae styles
        var normalStyle = document.Styles.OfType<IVStyle>().SingleOrDefault(x => x.Name == NormalStyleName) ??
                          document.Styles.Add(NormalStyleName, "", 1, 1, 1);
        normalStyle.CellsSRC[(short)VisSectionIndices.visSectionCharacter, (short)VisRowIndices.visRowCharacter,
            (short)VisCellIndices.visCharacterSize].FormulaU = "3mm";

        var pipelineStyle = document.Styles.OfType<IVStyle>().SingleOrDefault(x => x.Name == PipelineStyleName) ??
                            document.Styles.Add(PipelineStyleName, NormalStyleName, 1, 1, 1);


        if (font != null)
        {
            normalStyle.CellsSRC[(short)VisSectionIndices.visSectionCharacter, (short)VisRowIndices.visRowCharacter,
                (short)VisCellIndices.visCharacterFont].FormulaU = font.ID.ToString();
            normalStyle.CellsSRC[(short)VisSectionIndices.visSectionCharacter, (short)VisRowIndices.visRowCharacter,
                (short)VisCellIndices.visCharacterAsianFont].FormulaU = font.ID.ToString();
        }
        else
        {
            LogHost.Default.Warn(
                "Failed to setup the font becasue font is missing in the system.");
        }
    }

    public static void InsertWorkSheet(Worksheet worksheet)
    {
        var oleShape = Globals.ThisAddIn.Application.ActivePage.InsertObject("Excel.Sheet",
            (short)VisInsertObjArgs.visInsertAsEmbed);
        object oleObject = oleShape.Object;
        var workbook = (Workbook)oleObject;

        // 操作Excel对象
        (workbook.Worksheets[1] as Worksheet)?.Delete();
        workbook.Worksheets.Add(worksheet);

        // 保存并关闭Excel工作簿
        workbook.Save();
        workbook.Close(false); // 关闭工作簿，但不保存改变
        Marshal.ReleaseComObject(worksheet);
        Marshal.ReleaseComObject(workbook);
    }
}