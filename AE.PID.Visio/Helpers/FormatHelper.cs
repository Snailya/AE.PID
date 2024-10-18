using System;
using System.Linq;
using System.Windows.Forms;
using AE.PID.Visio.Exceptions;
using AE.PID.Visio.Extensions;
using Microsoft.Office.Interop.Visio;
using Splat;

namespace AE.PID.Visio.Helpers;

public abstract class FormatHelper
{
    public const string NormalStyleName = "AE Normal";
    private const string PipelineStyleName = "AE PipeLine";

    public static void FormatPage(Page page)
    {
        var document = page.Document;

        var undoScope = page.Application.BeginUndoScope("Format Document");

        // the grid setting is not relevant to any other custom object import by this program, so it should never have failed
        // so set it in the first.
        SetupGrid(page);

        try
        {
            // the style is strongly relevant to font 思源仿宋， if the font is missing, this step should be skipped.
            if (document.Fonts.OfType<Font>().SingleOrDefault(x => x.Name == "思源黑体") is { } font)
            {
                SetupStyles(document, font);
            }
            else
            {
                SetupStyles(document);
                MessageBox.Show("未找到思源黑体，将跳过字体设置,请在安装字体后重新初始化页面", "字体缺失", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }

            // insert the frame at the 0,0 potion
            InsertFrame(page);

            // set the view to make the frame center in the window
            Globals.ThisAddIn.Application.ActiveWindow.ViewFit = (int)VisWindowFit.visFitPage;

            document.EndUndoScope(undoScope, true);
        }
        catch (Exception ex)
        {
            document.EndUndoScope(undoScope, false);
            LogHost.Default.Error(ex, "Failed to format document.");
        }
    }

    private static void SetupGrid(IVPage page)
    {
        page.PageSheet.CellsSRC[(short)VisSectionIndices.visSectionObject, (short)VisRowIndices.visRowRulerGrid,
            (short)VisCellIndices.visXGridDensity].FormulaU = ((int)VisCellVals.visGridFixed).ToString();
        page.PageSheet.CellsSRC[(short)VisSectionIndices.visSectionObject, (short)VisRowIndices.visRowRulerGrid,
            (short)VisCellIndices.visYGridDensity].FormulaU = ((int)VisCellVals.visGridFixed).ToString();
        page.PageSheet.CellsSRC[(short)VisSectionIndices.visSectionObject, (short)VisRowIndices.visRowRulerGrid,
            (short)VisCellIndices.visXGridSpacing].FormulaU = "2.5mm";
        page.PageSheet.CellsSRC[(short)VisSectionIndices.visSectionObject, (short)VisRowIndices.visRowRulerGrid,
            (short)VisCellIndices.visYGridSpacing].FormulaU = "2.5mm";

        LogHost.Default.Info($"Grid setup for {page.Name} finished");
    }

    private static void InsertFrame(IVPage page)
    {
        const string baseId = "{7811D65E-9633-4E98-9FCD-B496A8B823A7}";

        try
        {
            var frameObject = page.Document.GetMaster(baseId);

            page.Drop(frameObject, 0, 0);
            page.AutoSizeDrawing();
        }
        catch (MasterNotValidException)
        {
            MessageBox.Show("未能找到图框，请检查AE逻辑.vssx文件。");
        }
        catch (Exception e)
        {
            LogHost.Default.Error(e,
                "Failed to insert frame at origin");
        }
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
}