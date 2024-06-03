using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
using System.Windows;
using AE.PID.Properties;
using AE.PID.Services;
using AE.PID.ViewModels;
using Microsoft.Office.Interop.Visio;
using Microsoft.Win32;
using Splat;
using Font = Microsoft.Office.Interop.Visio.Font;
using Path = System.IO.Path;
using SaveFileDialog = Microsoft.Win32.SaveFileDialog;

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
            WindowManager.ShowDialog(Resources.MSG_font_not_found, MessageBoxButton.OK);
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

    private static string GetUpdateToolPathFromRegistryKey()
    {
        const string registryKeyPath = @"Software\Microsoft\Visio\Addins\AE.PID";
        const string valueName = "Manifest";

        LogHost.Default.Info(
            $"Try find isntall path from register key. KeyPath: {registryKeyPath}. Value: {valueName}");

        using var registryKey = Registry.CurrentUser.OpenSubKey(registryKeyPath);
        var value = registryKey?.GetValue(valueName) as string;

        if (string.IsNullOrEmpty(value)) throw new RegistryKeyValueNotFoundException(registryKeyPath, valueName);

        var index = value!.IndexOf('|');
        if (index >= 0) value = value.Substring(0, index);

        var fileUri = new Uri(value);
        var filePath = fileUri.LocalPath;
        var folderPath = Path.GetDirectoryName(filePath)!;

        var toolPath = Path.Combine(folderPath,
            "DocumentStencilUpdateTool/PID.DocumentStencilUpdateTool.exe");
        if (!File.Exists(toolPath))
            throw new UpdateToolNotExistException(toolPath);

        LogHost.Default.Info($"The update tool is at {toolPath}");

        return toolPath;
    }

    private static void VerifySaved(Document document)
    {
        // first check if the document is a tmp document
        if (string.IsNullOrEmpty(document.Path))
        {
            // display a dialog to ask user to save the document
            var dialog = new SaveFileDialog
            {
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                Filter = @"Visio Files|*.vsdx",
                Title = @"保存文件"
            };
            if (dialog.ShowDialog() != true) throw new DocumentNotSavedException(); // user cancel
            document.SaveAs(dialog.FileName);
        }

        // save the document if it has changes
        if (document.Saved == false) document.Save();
    }

    /// <summary>
    ///     Save and close document, then update the stencil using PID.DocumentStencilUpdateTool.exe.
    /// </summary>
    /// <param name="document"></param>
    public static void UpdateDocument(Document document)
    {
        var progress = new Progress<ProgressValue>();
        WindowManager.GetInstance()!.CreateRunInBackgroundWithProgress(progress,
            () =>
            {
                var file = document.FullName;

                try
                {
                    Contract.Assert(document.Type == VisDocumentTypes.visTypeDrawing);
                    VerifySaved(document);

                    // close document before update
                    document.Close();

                    UpdateDocumentStencil(file, progress);
                }
                catch (Exception ex)
                {
                    LogHost.Default.Error(ex, "Failed to update document stencil.");
                    ((IProgress<ProgressValue>)progress).Report(new ProgressValue
                        { Message = ex.Message, Status = TaskStatus.OnError });
                }
                finally
                {
                    // reopen the file
                    Globals.ThisAddIn.Application.Documents.Open(file);
                }
            });
    }

    private static void UpdateDocumentStencil(string file, IProgress<ProgressValue> progress)
    {
        LogHost.Default.Info("Try updating the documents.");

        // find out the update tool
        var toolPath = GetUpdateToolPathFromRegistryKey();
        progress.Report(
            new ProgressValue
            {
                Message = string.Format(Resources.MSG_update_tool_found_at, toolPath),
                Status = TaskStatus.Running
            });

        // update using document stencil update tool
        var processStartInfo = new ProcessStartInfo
        {
            Verb = null,
            Arguments =
                $"--file \"{file}\" --reference \"{Path.Combine(Constants.LibraryFolder, ".cheatsheet")}\"",
            CreateNoWindow = true,
            RedirectStandardInput = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            Domain = null,
            LoadUserProfile = false,
            FileName = $"\"{toolPath}\"",
            ErrorDialog = false
        };

        LogHost.Default.Info(
            $"Update document through command line: {processStartInfo.FileName} {processStartInfo.Arguments}");

        var process = new Process
        {
            StartInfo = processStartInfo,
            EnableRaisingEvents = true
        };

        // redirect info and error
        process.OutputDataReceived += (_, e) =>
        {
            if (e.Data != null)
            {
                LogHost.Default.Info(e.Data);

                progress.Report(
                    new ProgressValue
                        { Message = e.Data, Status = TaskStatus.Running });
            }
        };
        process.ErrorDataReceived += (_, e) =>
        {
            if (e.Data != null) throw new ProcessErrorException(e.Data);
        };

        // start the process and wait to complete
        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        process.WaitForExit();

        if (process.ExitCode == 0)
        {
            LogHost.Default.Info("Document update completed.");

            progress.Report(new ProgressValue
                { Message = Resources.MSG_update_completed, Status = TaskStatus.RanToCompletion });
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

    private class RegistryKeyValueNotFoundException(string registryKeyPath, string valueName)
        : Exception($"{valueName} not exist in {registryKeyPath}.");

    private class UpdateToolNotExistException(string toolPath)
        : Exception($"Unable to find the {toolPath}.");

    private class ProcessErrorException(string data)
        : Exception(data);

    private class DocumentNotSavedException : Exception;
}