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
                InsertPCITables(page, frame);

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

    public const string FrameBaseId = "{7811D65E-9633-4E98-9FCD-B496A8B823A7}";

    private static Shape? InsertFrameIfNotExist(IVPage page)
    {

        var frame = page.Shapes.OfType<Shape>().FirstOrDefault(x => x.Master.BaseID == FrameBaseId);

        if (frame != null) return frame;

        try
        {
            var frameObject = page.Document.GetMaster(FrameBaseId);

            frame = page.DropMetric(frameObject, (0, 0));
            page.AutoSizeDrawing();

            return frame;
        }
        catch (MasterNotValidException)
        {
            MessageBox.Show(@"未能找到图框，请检查AE逻辑.vssx文件。", "初始化");
        }
        catch (Exception e)
        {
            LogHost.Default.Error(e,
                "Failed to insert frame at origin");
        }

        return null;
    }

    public static (Shape? Table1, Shape? Table2) InsertPCITables(IVPage page, Shape frame)
    {
        const string table1BaseId = "{D1A49D75-2A8B-4F4B-9A3A-27A0BC63D08D}";
        const string table2BaseId = "{4B7CA2AC-E82E-4382-80F8-D2E0CC85B151}";

        Shape? table1 = null;
        Shape? table2 = null;

        try
        {
            var frameBox = frame.BoundingBoxMetric((short)VisBoundingBoxArgs.visBBoxDrawingCoords +
                                                   (short)VisBoundingBoxArgs.visBBoxExtents);
            if (!page.Shapes.OfType<Shape>().Any(x =>
                    x.Master?.BaseID == table1BaseId &&
                    x.BoundingBoxInside((short)VisBoundingBoxArgs.visBBoxExtents, frameBox)))
            {
                var table1Object = page.Document.GetMaster(table1BaseId);
                table1 = page.DropMetric(table1Object, (frameBox.Right - 130, frameBox.Top - 63));

                // set the table location bind to frame
                var srcStream1 = Array.CreateInstance(typeof(short), 6);
                srcStream1.SetValue((short)VisSectionIndices.visSectionObject, 0);
                srcStream1.SetValue((short)VisRowIndices.visRowXFormOut, 1);
                srcStream1.SetValue((short)VisCellIndices.visXFormPinX, 2);

                srcStream1.SetValue((short)VisSectionIndices.visSectionObject, 3);
                srcStream1.SetValue((short)VisRowIndices.visRowXFormOut, 4);
                srcStream1.SetValue((short)VisCellIndices.visXFormPinY, 5);

                var formulas1 = Array.CreateInstance(typeof(object), 2);
                formulas1.SetValue($"=Sheet.{frame.ID}!PinX + Sheet.{frame.ID}!Width - 10 mm - Width * 0.5", 0);
                formulas1.SetValue($"=Sheet.{frame.ID}!PinY + Sheet.{frame.ID}!Height - 10 mm - Height * 0.5", 1);

                table1.SetFormulas(ref srcStream1, ref formulas1, 0);
            }
            else
            {
                table1 = page.Shapes.OfType<Shape>().SingleOrDefault(x =>
                    x.Master.BaseID == table1BaseId &&
                    x.BoundingBoxInside((short)VisBoundingBoxArgs.visBBoxExtents, frameBox));
            }

            if (!page.Shapes.OfType<Shape>().Any(x =>
                    x.Master?.BaseID == table2BaseId &&
                    x.BoundingBoxInside((short)VisBoundingBoxArgs.visBBoxExtents, frameBox)))
            {
                var table2Object = page.Document.GetMaster(table2BaseId);

                table2 = page.DropMetric(table2Object, (frameBox.Right - 130, frameBox.Top - 229));
                
                var srcStream2 = Array.CreateInstance(typeof(short), 6);
                srcStream2.SetValue((short)VisSectionIndices.visSectionObject, 0);
                srcStream2.SetValue((short)VisRowIndices.visRowXFormOut, 1);
                srcStream2.SetValue((short)VisCellIndices.visXFormPinX, 2);

                srcStream2.SetValue((short)VisSectionIndices.visSectionObject, 3);
                srcStream2.SetValue((short)VisRowIndices.visRowXFormOut, 4);
                srcStream2.SetValue((short)VisCellIndices.visXFormPinY, 5);

                var formulas2 = Array.CreateInstance(typeof(object), 2);
                formulas2.SetValue($"=Sheet.{frame.ID}!PinX + Sheet.{frame.ID}!Width - 10 mm - Width * 0.5", 0);
                formulas2.SetValue(
                    $"=Sheet.{frame.ID}!PinY + Sheet.{frame.ID}!Height - 10 mm - Sheet.{table1.ID}!Height - Height * 0.5",
                    1);

                table2.SetFormulas(ref srcStream2, ref formulas2, 0);
            }
            
            return (table1, table2);
        }
        catch (MasterNotValidException)
        {
            MessageBox.Show(@"未能找到PCI符号说明，请检查AE标识.vssx文件是否已打开。", "初始化");
        }
        catch (Exception e)
        {
            LogHost.Default.Error(e,
                "Failed to insert frame at origin");
        }

        return (table1, table2);
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