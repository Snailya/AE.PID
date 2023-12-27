using System;
using System.Linq;
using System.Reactive.Subjects;
using Microsoft.Office.Interop.Visio;
using NLog;

namespace AE.PID.Controllers.Services;

public static class DocumentInitializer
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
    private static Subject<IVDocument> ManuallyInvokeTrigger { get; } = new();

    /// <summary>
    ///     Trigger manually.
    /// </summary>
    public static void Invoke(IVDocument document)
    {
        ManuallyInvokeTrigger.OnNext(document);
    }

    /// <summary>
    /// Start listening to the document initialize button clicked.
    /// Setup theme font and size, ruler and grid.
    /// </summary>
    /// <returns></returns>
    public static IDisposable Listen()
    {
        Logger.Info($"Document Initialize Service started.");

        return ManuallyInvokeTrigger.Subscribe(Initialize,
            ex =>
            {
                ThisAddIn.Alert(ex.Message);
                Logger.Error(ex,
                    $"Document Initialize Service terminated accidentally.");
            },
            () => { Logger.Error("Document Initialize Service should never complete."); });
    }

    private static void Initialize(IVDocument document)
    {
        var undoScope = document.Application.BeginUndoScope("Initialize Document");

        // get font id
        var font = document.Fonts.OfType<Font>().SingleOrDefault(x => x.Name == "思源黑体");

        if (font == null)
        {
            ThisAddIn.Alert("未找到思源黑体，请确认安装完成后重启Visio。");
            return;
        }

        var themeStyle = document.Styles.ItemU["Theme"];
        themeStyle.CellsSRC[(short)VisSectionIndices.visSectionCharacter, (short)VisRowIndices.visRowCharacter,
            (short)VisCellIndices.visCharacterFont].FormulaU = font.ID.ToString();
        themeStyle.CellsSRC[(short)VisSectionIndices.visSectionCharacter, (short)VisRowIndices.visRowCharacter,
            (short)VisCellIndices.visCharacterAsianFont].FormulaU = font.ID.ToString();
        themeStyle.CellsSRC[(short)VisSectionIndices.visSectionCharacter, (short)VisRowIndices.visRowCharacter,
            (short)VisCellIndices.visCharacterSize].FormulaU = "3mm";

        // setup grid and ruler
        foreach (var page in document.Pages.OfType<IVPage>())
        {
            page.PageSheet.CellsSRC[(short)VisSectionIndices.visSectionObject, (short)VisRowIndices.visRowRulerGrid,
                (short)VisCellIndices.visXGridDensity].FormulaU = "0";
            page.PageSheet.CellsSRC[(short)VisSectionIndices.visSectionObject, (short)VisRowIndices.visRowRulerGrid,
                (short)VisCellIndices.visYGridDensity].FormulaU = "0";
            page.PageSheet.CellsSRC[(short)VisSectionIndices.visSectionObject, (short)VisRowIndices.visRowRulerGrid,
                (short)VisCellIndices.visXGridSpacing].FormulaU = "2.5mm";
            page.PageSheet.CellsSRC[(short)VisSectionIndices.visSectionObject, (short)VisRowIndices.visRowRulerGrid,
                (short)VisCellIndices.visYGridSpacing].FormulaU = "2.5mm";
        }

        document.EndUndoScope(undoScope, true);
    }
}