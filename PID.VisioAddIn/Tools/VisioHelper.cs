using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using AE.PID.Controllers;
using Microsoft.Office.Interop.Visio;
using NLog;
using Path = System.IO.Path;

namespace AE.PID.Tools;

public static class VisioHelper
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    /// <summary>
    ///     Load library's in config into Visio stencils.
    /// </summary>
    public static void OpenLibraries()
    {
        try
        {
            var configuration = ServiceManager.GetInstance().Configuration;

            foreach (var path in configuration.Libraries.Items.Select(x => x.Path))
                Globals.ThisAddIn.Application.Documents.OpenEx(path, (short)VisOpenSaveArgs.visOpenDocked);

            Logger.Info($"Opened {configuration.Libraries.Count} libraries.");
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Failed to open libaries.");
        }
    }

    /// <summary>
    /// </summary>
    /// <param name="document"></param>
    public static void FormatDocument(IVDocument document)
    {
        var undoScope = document.Application.BeginUndoScope("Format Document");

        try
        {
            SetupStyles(document);

            foreach (var page in document.Pages.OfType<IVPage>())
                SetupGrid(page);

            document.EndUndoScope(undoScope, true);

            Logger.Info($"Formated {document.Name} successfully.");
        }
        catch (Exception ex)
        {
            document.EndUndoScope(undoScope, false);
            Logger.Error(ex, "Failed to format document.");
        }
    }

    private static void SetupStyles(IVDocument document)
    {
        // verify font exist
        var font = document.Fonts.OfType<Font>().SingleOrDefault(x => x.Name == "思源黑体");

        if (font == null)
        {
            ThisAddIn.Alert("未找到思源黑体，请确认安装完成后重启Visio。");
            return;
        }

        // setup or initialize ae styles
        const string normalStyleName = "AE Normal";
        var normalStyle = document.Styles.OfType<IVStyle>().SingleOrDefault(x => x.Name == normalStyleName) ??
                          document.Styles.Add(normalStyleName, "", 1, 1, 1);
        normalStyle.CellsSRC[(short)VisSectionIndices.visSectionCharacter, (short)VisRowIndices.visRowCharacter,
            (short)VisCellIndices.visCharacterFont].FormulaU = font.ID.ToString();
        normalStyle.CellsSRC[(short)VisSectionIndices.visSectionCharacter, (short)VisRowIndices.visRowCharacter,
            (short)VisCellIndices.visCharacterAsianFont].FormulaU = font.ID.ToString();
        normalStyle.CellsSRC[(short)VisSectionIndices.visSectionCharacter, (short)VisRowIndices.visRowCharacter,
            (short)VisCellIndices.visCharacterSize].FormulaU = "3mm";

        const string pipelineStyleName = "AE PipeLine";
        var pipelineStyle = document.Styles.OfType<IVStyle>().SingleOrDefault(x => x.Name == pipelineStyleName) ??
                            document.Styles.Add(pipelineStyleName, normalStyleName, 1, 1, 1);

        Logger.Info($"Style setup for {document.Name} finished");
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

        Logger.Info($"Grid setup for {page.Name} finished");
    }

    /// <summary>
    ///     Save and close document, then update the stencil using PID.DocumentStencilUpdateTool.exe.
    /// </summary>
    /// <param name="document"></param>
    public static void UpdateDocumentStencil(Document document)
    {
        try
        {
            Contract.Assert(document.Type == VisDocumentTypes.visTypeDrawing);

            var file = document.FullName;

            // save changes if it has unsaved changes
            if (document.Saved == false) document.Save();

            // close document if it is opened
            if (document.Stat != (short)VisStatCodes.visStatClosed) document.Close();

            // update using document stencil update tool
            var processStartInfo = new ProcessStartInfo
            {
                Verb = null,
                Arguments = $"--file {file} --reference {Path.Combine(Constants.LibraryFolder, ".cheatsheet")}",
                CreateNoWindow = true,
                RedirectStandardInput = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                Domain = null,
                LoadUserProfile = false,
                FileName = "./DocumentStencilUpdateTool/PID.DocumentStencilUpdateTool.exe",
                ErrorDialog = false
            };

            var process = new Process
            {
                StartInfo = processStartInfo,
                EnableRaisingEvents = true
            };

            process.OutputDataReceived += (sender, args) => { Logger.Info(args.Data); };
            process.ErrorDataReceived += (sender, e) =>
            {
                if (e.Data != null)
                    Logger.Error(e.Data);
            };
            process.Exited += (sender, args) =>
            {
                if (sender is Process { ExitCode: 0 })
                    ThisAddIn.Alert("更新成功");
                else
                    ThisAddIn.Alert("更新失败");

                Globals.ThisAddIn.Application.Documents.OpenEx(file, (short)OpenFlags.ReadWrite);
            };

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Failed to update document stencil.");
        }
    }

    public static IEnumerable<Document> CloseOpenedStencils()
    {
        var stencils = Globals.ThisAddIn.Application.Documents
            .OfType<Document>()
            .Where(x => x.Type == VisDocumentTypes.visTypeStencil).ToList();

        foreach (var item in stencils)
            item.Close();

        return stencils;
    }
}