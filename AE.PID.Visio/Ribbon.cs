using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Forms;
using AE.PID.Core.DTOs;
using AE.PID.Core.Models;
using AE.PID.Visio.Core.Exceptions;
using AE.PID.Visio.Core.Interfaces;
using AE.PID.Visio.Extensions;
using AE.PID.Visio.Helpers;
using AE.PID.Visio.Shared;
using AE.PID.Visio.UI.Avalonia.ViewModels;
using AE.PID.Visio.UI.Avalonia.Views;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Office.Interop.Excel;
using Microsoft.Office.Interop.Visio;
using Splat;
using Office = Microsoft.Office.Core;
using Path = System.IO.Path;
using Shape = Microsoft.Office.Interop.Visio.Shape;

// TODO:  Follow these steps to enable the Ribbon (XML) item:

// 1: Copy the following code block into the ThisAddin, ThisWorkbook, or ThisDocument class.

//  protected override Microsoft.Office.Core.IRibbonExtensibility CreateRibbonExtensibilityObject()
//  {
//      return new Ribbon2();
//  }

// 2. Create callback methods in the "Ribbon Callbacks" region of this class to handle user
//    actions, such as clicking a button. Note: if you have exported this Ribbon from the Ribbon designer,
//    move your code from the event handlers to the callback methods and modify the code to work with the
//    Ribbon extensibility (RibbonX) programming model.

// 3. Assign attributes to the control tags in the Ribbon XML file to identify the appropriate callback methods in your code.  

// For more information, see the Ribbon XML documentation in the Visual Studio Tools for Office Help.


namespace AE.PID.Visio;

[ComVisible(true)]
public class Ribbon : Office.IRibbonExtensibility
{
    private Office.IRibbonUI _ribbon;

    #region IRibbonExtensibility Members

    public string GetCustomUI(string ribbonID)
    {
        return GetResourceText("AE.PID.Visio.Ribbon.xml");
    }

    #endregion

    #region Helpers

    private static string GetResourceText(string resourceName)
    {
        var asm = Assembly.GetExecutingAssembly();
        var resourceNames = asm.GetManifestResourceNames();
        for (var i = 0; i < resourceNames.Length; ++i)
            if (string.Compare(resourceName, resourceNames[i], StringComparison.OrdinalIgnoreCase) == 0)
                using (var resourceReader = new StreamReader(asm.GetManifestResourceStream(resourceNames[i])))
                {
                    if (resourceReader != null) return resourceReader.ReadToEnd();
                }

        return null;
    }

    #endregion

    private bool IsSingleShapeSelection()
    {
        var window = Globals.ThisAddIn.Application.ActiveWindow;
        // if not the drawing page
        if (window.Document.Type != VisDocumentTypes.visTypeDrawing) return false;
        // if multiple selection
        if (window.Selection.Count != 1) return false;

        return true;
    }

    #region -- Setting Group --

    public void OpenSettings(Office.IRibbonControl control)
    {
        WindowHelper.Show<SettingsWindow, SettingsWindowViewModel>();
    }

    #endregion

    #region -- Project Group --

    public void OpenProjectExplorer(Office.IRibbonControl control)
    {
        WindowHelper.Show<ProjectExplorerWindow, ProjectExplorerWindowViewModel>();
    }

    #endregion

    public void Debug(Office.IRibbonControl control)
    {
        var dataArray = new string[2, 9]
        {
            { "1", "2", "3", "4", "5", "6", "7", "8", "9" },
            { "1", "2", "3", "4", "5", "6", "7", "8", "9" }
        };

        var worksheet = new Worksheet();
        worksheet.Range["A1", "C1"].Merge();
        worksheet.Range["A1"].Value2 = "Letter code for process variables and control functions (ISO 15519-2)";

        FormatHelper.InsertWorkSheet(worksheet);
    }


    #region -- Ribbon Callbacks --

    //Create callback methods here. For more information about adding callback methods, visit https://go.microsoft.com/fwlink/?LinkID=271226

    public void Ribbon_Load(Office.IRibbonUI ribbonUI)
    {
        _ribbon = ribbonUI;

        // register on triggers to update button status.
        RegisterUpdateForElements();
    }

    /// <summary>
    ///     Because the state of the buttons on ribbon will not re-compute once loaded.
    ///     So the re-computation needs to be triggered manually by calling _ribbon.Invalidate().
    ///     As the button state is related to if there is a document in open state, observe on these two events.
    /// </summary>
    private void RegisterUpdateForElements()
    {
        Globals.ThisAddIn.Application.WindowOpened += _ => { _ribbon.Invalidate(); };
        Globals.ThisAddIn.Application.WindowChanged += _ => { _ribbon.Invalidate(); };
    }

    #endregion

    #region -- Design Group --

    public void LoadLibraries(Office.IRibbonControl control)
    {
        LibraryHelper.OpenLibraries();
    }

    public bool IsLoadLibrariesValid(Office.IRibbonControl control)
    {
        return Globals.ThisAddIn.Application.ActiveDocument != null;
    }

    public void FormatPage(Office.IRibbonControl control)
    {
        FormatHelper.FormatPage(Globals.ThisAddIn.Application.ActivePage);
    }

    public bool IsFormatPageValid(Office.IRibbonControl control)
    {
        return Globals.ThisAddIn.Application.ActiveDocument != null;
    }

    public void InsertLegend(Office.IRibbonControl control)
    {
        LegendHelper.Insert(Globals.ThisAddIn.Application.ActivePage);
    }

    public bool IsInsertLegendValid(Office.IRibbonControl control)
    {
        return true;
    }

    public async Task UpdateDocument(Office.IRibbonControl control)
    {
        var service = ThisAddIn.Services.GetRequiredService<IDocumentUpdateService>();

        var doc = Globals.ThisAddIn.Application.ActiveDocument;

        //remove hidden information to reduce size
        doc.RemoveHiddenInformation((int)VisRemoveHiddenInfoItems.visRHIMasters);

        // 文档可能有两种情况：
        // 1. 文档是新建的，此时没有FullName，但是这种情况不应该发生，因为没有检查一个新建的文档，因为该文档随时可被丢弃。
        // 2. 文档曾经被保存过，此时才有检查更新的必要
        // 所以此处假定文档一定有fullname。
        var filePath = doc.FullName;
        doc.Close();

        try
        {
            // do update
            await service.UpdateAsync(filePath);
        }
        catch (DocumentNotRecognizedException e)
        {
            MessageBox.Show(@"更新失败，文档无法被识别。", "文档更新");
        }
        catch (Exception e)
        {
            MessageBox.Show($"更新失败，{e.Message}", "文档更新");
        }

        // reopen after updated
        Globals.ThisAddIn.Application.Documents.Add(filePath);
    }

    public bool IsUpdateDocumentValid(Office.IRibbonControl control)
    {
        if (Globals.ThisAddIn.Application.ActiveDocument == null) return false;

        // check if the document is the visDrawing, not the stencil or other type
        if (Globals.ThisAddIn.Application.ActiveDocument.Type != VisDocumentTypes.visTypeDrawing) return false;

        // 如果文档从来没有被存储过，则不检查
        if (!Path.IsPathRooted(Globals.ThisAddIn.Application.ActiveDocument.FullName)) return false;

        // check if the AE style exist, if the AE style exist, means this is a target drawing.
        if (Globals.ThisAddIn.Application.ActiveDocument.Styles.OfType<IVStyle>()
                .SingleOrDefault(x => x.Name == FormatHelper.NormalStyleName) ==
            null) return false;

        LogHost.Default.Info(
            $"Checking if the masters in {Globals.ThisAddIn.Application.ActiveDocument.FullName} is up to date...");

        // check if the version is out of date
        var masters = Globals.ThisAddIn.Application.ActiveDocument.Masters.OfType<IVMaster>().Select(x =>
            new MasterSnapshotDto
            {
                BaseId = x.BaseID,
                UniqueId = x.UniqueID
            });

        var service = ThisAddIn.Services.GetRequiredService<IDocumentUpdateService>();
        var needUpdate = service.HasUpdate(masters);
        LogHost.Default.Info(needUpdate
            ? $"{Globals.ThisAddIn.Application.ActiveDocument.FullName} need update."
            : $"{Globals.ThisAddIn.Application.ActiveDocument.FullName} is up to date.");

        return needUpdate;
    }

    public void OpenTools(Office.IRibbonControl control)
    {
        WindowHelper.Show<ToolsWindow, ToolsWindowViewModel>();
    }

    #endregion

    #region -- Check Group --

    public void ValidateDesignationUnique(Office.IRibbonControl control)
    {
        ErrorHelper.CheckDesignationUnique(Globals.ThisAddIn.Application.ActivePage);
        _ribbon.Invalidate();
    }

    public void ValidateMasterExist(Office.IRibbonControl control)
    {
        ErrorHelper.ScanMaster(Globals.ThisAddIn.Application.ActivePage);
        _ribbon.Invalidate();
    }

    public void ClearValidationMarks(Office.IRibbonControl control)
    {
        ErrorHelper.ClearCheckMarks(Globals.ThisAddIn.Application.ActivePage);
    }

    public bool IsValidateDesignationUniqueValid(Office.IRibbonControl control)
    {
        return Globals.ThisAddIn.Application.ActiveDocument != null && !IsClearValidationMarksValid(control);
    }

    public bool IsValidateMasterExistValid(Office.IRibbonControl control)
    {
        return Globals.ThisAddIn.Application.ActiveDocument != null && !IsClearValidationMarksValid(control);
    }

    public bool IsClearValidationMarksValid(Office.IRibbonControl control)
    {
        if (Globals.ThisAddIn.Application.ActivePage == null) return false;

        var selection = Globals.ThisAddIn.Application.ActivePage.CreateSelection(VisSelectionTypes.visSelTypeByLayer,
            VisSelectMode.visSelModeSkipSuper, ErrorHelper.ValidationLayerName);
        return selection.Count > 0;
    }

    #endregion

    #region -- Material Context Menu --

    public void ShowMaterialDataPane(Office.IRibbonControl control)
    {
        WindowHelper.ShowTaskPane<MaterialPaneView, MaterialPaneViewModel>("物料",
            (shape, vm) =>
            {
                System.Diagnostics.Debug.WriteLine($"selected shape id : {shape.ID}");
                vm.Code = shape.TryGetValue(CellNameDict.MaterialCode) ?? string.Empty;
            });
    }

    public void DeleteDesignMaterial(Office.IRibbonControl control)
    {
        foreach (var shape in Globals.ThisAddIn.Application.ActiveWindow.Selection.OfType<Shape>())
        {
            if (!shape.CellExistsN(CellNameDict.MaterialCode, VisExistsFlags.visExistsLocally)) continue;
            shape.TrySetValue(CellNameDict.MaterialCode, "");
        }
    }

    public bool HasMaterial(Office.IRibbonControl control)
    {
        var selected = Globals.ThisAddIn.Application.ActiveWindow.Selection.OfType<IVShape>();
        return selected.Any(x =>
            x.CellExistsN(CellNameDict.MaterialCode, VisExistsFlags.visExistsLocally) &&
            !string.IsNullOrEmpty(x.Cells[CellNameDict.MaterialCode].ResultStr[VisUnitCodes.visUnitsString]));
    }

    public bool AreMaterialLocations(Office.IRibbonControl control)
    {
        return Globals.ThisAddIn.Application.ActiveWindow.Selection.OfType<IVShape>()
            .All(x => x.HasCategory("Equipment") || x.HasCategory("Instrument") || x.HasCategory("FunctionalElement"));
    }
    
    #endregion

    #region -- Proxy Context Menu --

    public void InsertFunctionGroup(Office.IRibbonControl control)
    {
        var target = Globals.ThisAddIn.Application.ActiveWindow.Selection[1];
        ProxyHelper.Insert(target, FunctionType.FunctionGroup);
    }

    public bool IsInsertFunctionGroupValid(Office.IRibbonControl control)
    {
        if (!IsSingleShapeSelection()) return false;

        return Globals.ThisAddIn.Application.ActiveWindow.Selection.OfType<IVShape>()
            .All(x => x.HasCategory("FunctionalGroup"));
    }

    public void InsertEquipment(Office.IRibbonControl control)
    {
        var target = Globals.ThisAddIn.Application.ActiveWindow.Selection[1];
        ProxyHelper.Insert(target, FunctionType.Equipment);
    }

    public bool IsInsertEquipmentValid(Office.IRibbonControl control)
    {
        if (!IsSingleShapeSelection()) return false;

        return Globals.ThisAddIn.Application.ActiveWindow.Selection.OfType<IVShape>()
            .All(x => !x.HasCategory("Frame") && !x.HasCategory("FunctionalGroup") && !x.HasCategory("Equipment") &&
                                                         !x.HasCategory("Instrument") &&
                                                         !x.HasCategory("FunctionalElement"));
    }

    public void InsertFunctionElement(Office.IRibbonControl control)
    {
        var target = Globals.ThisAddIn.Application.ActiveWindow.Selection[1];
        ProxyHelper.Insert(target, FunctionType.FunctionElement);
    }

    public bool IsInsertFunctionElementValid(Office.IRibbonControl control)
    {
        if (!IsSingleShapeSelection()) return false;

        return Globals.ThisAddIn.Application.ActiveWindow.Selection.OfType<IVShape>()
            .All(x => x.HasCategory("Equipment") || (x.HasCategory("Instrument") && !x.HasCategory("Proxy")));
    }

    #endregion

    #region -- Frame Context Menu --

    public void InsertPCIDescription(Office.IRibbonControl control)
    {
        var frame = Globals.ThisAddIn.Application.ActiveWindow.Selection[1];
        FormatHelper.InsertPCITables(frame.ContainingPage, frame!);
    }

    public bool IsFrame(Office.IRibbonControl control)
    {
        if (!IsSingleShapeSelection()) return false;

        var selection = Globals.ThisAddIn.Application.ActiveWindow.Selection[1];
        return selection != null && selection.Master.BaseID == FormatHelper.FrameBaseId;
    }

    #endregion
}