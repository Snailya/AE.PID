using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Reactive.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Windows;
using AE.PID.Services;
using AE.PID.ViewModels;
using Microsoft.Office.Interop.Visio;
using Splat;
using Path = System.IO.Path;

namespace AE.PID.Tools;

internal static class VisioHelper
{
    /// <summary>
    ///     Load library's in config into Visio stencils.
    /// </summary>
    public static void OpenLibraries(List<string> paths)
    {
        try
        {
            foreach (var path in paths)
                Globals.ThisAddIn.Application.Documents.OpenEx(path, (short)VisOpenSaveArgs.visOpenDocked);

            LogHost.Default.Info($"Opened {paths.Count} libraries.");
        }
        catch (Exception ex)
        {
            LogHost.Default.Error(ex, "Failed to open libraries.");
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
            {
                SetupGrid(page);
                InsertFrame(page);
                Globals.ThisAddIn.Application.ActiveWindow.ViewFit = (int)VisWindowFit.visFitPage;
            }

            document.EndUndoScope(undoScope, true);

            LogHost.Default.Info($"Formatted {document.Name} successfully.");
        }
        catch (Exception ex)
        {
            document.EndUndoScope(undoScope, false);
            LogHost.Default.Error(ex, "Failed to format document.");
        }
    }


    private static void SetupStyles(IVDocument document)
    {
        // verify font exist
        var font = document.Fonts.OfType<Font>().SingleOrDefault(x => x.Name == "思源黑体");

        if (font == null)
        {
            WindowManager.ShowDialog("未找到思源黑体，请确认安装完成后重启Visio。", MessageBoxButton.OK);
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

        LogHost.Default.Info($"Style setup for {document.Name} finished");
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
        // ensure document opened
        var document = page.Application.Documents.OfType<Document>().SingleOrDefault(x => x.Name == "AE逻辑.vssx");
        if (document == null)
        {
            var configuration = Locator.Current.GetService<ConfigurationService>()!;
            var documentPath = configuration.Libraries.Lookup(6).Value.Path;
            document = page.Application.Documents.OpenEx(documentPath, (short)VisOpenSaveArgs.visOpenDocked);
        }

        var frameObject = document.Masters["B{7811D65E-9633-4E98-9FCD-B496A8B823A7}"];
        if (frameObject == null) return;

        page.Drop(frameObject, 0, 0);
        page.AutoSizeDrawing();
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

            var progress = new Progress<ProgressValue>();

            WindowManager.Dispatcher!.BeginInvoke(() =>
            {
                var progressViewModel = new ProgressPageViewModel { IsIndeterminate = true };

                Observable.FromEventPattern<ProgressValue>(handler => progress.ProgressChanged += handler,
                        handler => progress.ProgressChanged -= handler)
                    .Select(x => x.EventArgs)
                    .ObserveOn(WindowManager.Dispatcher)
                    .Subscribe(v => { progressViewModel.ProgressValue = v; });

                WindowManager.GetInstance()!.ShowProgressBar(progressViewModel);
            });

            var file = document.FullName;

            // save changes if it has unsaved changes
            if (document.Saved == false) document.Save();

            // close the document if it is opened
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

            process.OutputDataReceived += (sender, args) =>
            {
                ((IProgress<ProgressValue>)progress).Report(new ProgressValue
                    { Message = args.Data, Status = TaskStatus.Running });
                LogHost.Default.Info(args.Data);
            };
            process.ErrorDataReceived += (sender, e) =>
            {
                if (e.Data != null)
                    LogHost.Default.Error(e.Data);
            };
            process.Exited += (sender, args) =>
            {
                ((IProgress<ProgressValue>)progress).Report(new ProgressValue
                    { Message = string.Empty, Status = TaskStatus.RanToCompletion });

                if (sender is Process { ExitCode: 0 })
                    WindowManager.ShowDialog("更新成功", MessageBoxButton.OK);
                else
                    WindowManager.ShowDialog("更新失败", MessageBoxButton.OK);

                Globals.ThisAddIn.Application.Documents.OpenEx(file, (short)OpenFlags.ReadWrite);
            };

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
        }
        catch (Exception ex)
        {
            LogHost.Default.Error(ex, "Failed to update document stencil.");
        }
    }

    public static void InsertLegend(IVPage page)
    {
        try
        {
            LegendService.Insert(page);
        }
        catch (Exception ex)
        {
            LogHost.Default.Error(ex, "Failed to insert legend.");
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