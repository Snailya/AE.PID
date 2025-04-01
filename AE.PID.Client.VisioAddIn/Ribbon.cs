using System;
using System.IO;
using System.IO.Packaging;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml.Linq;
using AE.PID.Client.Core;
using AE.PID.Client.Core.Exceptions;
using AE.PID.Client.Core.VisioExt;
using AE.PID.Client.UI.Avalonia;
using AE.PID.Client.UI.Avalonia.VisioExt;
using AE.PID.Core;
using Microsoft.Extensions.DependencyInjection;
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


namespace AE.PID.Client.VisioAddIn;

[ComVisible(true)]
public class Ribbon : Office.IRibbonExtensibility
{
    private Office.IRibbonUI _ribbon;

    #region IRibbonExtensibility Members

    public string GetCustomUI(string ribbonID)
    {
        return GetResourceText("AE.PID.Client.VisioAddIn.Ribbon.xml");
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
        return window.Selection.Count == 1;
    }

    #region -- Setting Group --

    public void OpenSettings(Office.IRibbonControl control)
    {
        var vm = ThisAddIn.Services.GetRequiredService<SettingsWindowViewModel>();
        var ui = ThisAddIn.Services.GetRequiredService<IUserInteractionService>();

        ui.Show(vm, ThisAddIn.GetApplicationHandle());
    }

    #endregion

    public async void Debug(Office.IRibbonControl control)
    {
    }

    public void PasteShapeData(Office.IRibbonControl control)
    {
        var selection = Globals.ThisAddIn.Application.ActiveWindow.Selection[1]!;

        const string format = "Visio 15.0 Shapes";

        var memoryStream = (MemoryStream)Clipboard.GetDataObject()!.GetData(format);
        memoryStream.Seek(0, SeekOrigin.Begin);

        using var package = Package.Open(memoryStream, FileMode.Open, FileAccess.Read);
        var uri = new Uri("/visio/pages/page1.xml", UriKind.Relative);
        var page1Part = package.GetPart(uri);

        using var partStream = page1Part.GetStream();
        var partXml = XDocument.Load(partStream);
        XNamespace mainNs = @"http://schemas.microsoft.com/office/visio/2012/main";

        foreach (var cell in (partXml.Root.Element(mainNs + "Shapes").Element(mainNs + "Shape")
                     .Elements(mainNs + "Section")
                     .SingleOrDefault(i => i.Attribute("N")!.Value == "Property")?
                     .Elements(mainNs + "Row")
                     .Where(i => i.Attribute("N")?.Value != "FunctionalElement" &&
                                 i.Attribute("N")?.Value != "Class" &&
                                 i.Attribute("N")?.Value != "SubClass")
                     .Select(x =>
                         x.Elements(mainNs + "Cell").SingleOrDefault(i => i.Attribute("N")?.Value == "Value"))
                     .Where(x => x != null))
                 .Where(x => !string.IsNullOrWhiteSpace(x.Attribute("V")?.Value)))
        {
            var name = $"Prop.{cell!.Parent!.Attribute("N")!.Value}";
            var value = cell.Attribute("V")!.Value;

            selection.TrySetValue(name, value);
        }
    }

    public bool CanPasteShapeData(Office.IRibbonControl control)
    {
        if (!IsSingleShapeSelection()) return false;

        // if it is a visio shape in clipboard
        const string format = "Visio 15.0 Shapes";
        var clipboardData = Clipboard.GetDataObject();
        if (clipboardData == null) return false;
        if (!clipboardData.GetDataPresent(format)) return false;

        // check if there is only one shape copied
        var memoryStream = (MemoryStream)clipboardData.GetData(format);
        memoryStream.Seek(0, SeekOrigin.Begin);

        using var package = Package.Open(memoryStream, FileMode.Open, FileAccess.Read);
        var uri = new Uri("/visio/pages/page1.xml", UriKind.Relative);
        var page1Part = package.GetPart(uri);

        using var partStream = page1Part.GetStream();
        var partXml = XDocument.Load(partStream);
        XNamespace mainNs = @"http://schemas.microsoft.com/office/visio/2012/main";
        return partXml.Root?.Element(mainNs + "Shapes")?.Elements(mainNs + "Shape").Count() == 1;
    }

    public string GetSuggestionContent(Office.IRibbonControl control)
    {
        var content = @"
    <menu xmlns='http://schemas.microsoft.com/office/2009/07/customui'>
      <button id='buttonA' label='Dynamic Button 1' />
      <button id='buttonB' label='Dynamic Button 2' />
    </menu>";
        return content;
    }

    #region -- Fix Group --

    public void FixEndOfFile(Office.IRibbonControl control)
    {
        // 2025.3.17：出现意外的文件尾错误时，将文件另存为兼容格式再另存为原格式。
        var fileName = Globals.ThisAddIn.Application.ActiveDocument.FullName;
        var compatibilityFileName = Path.ChangeExtension(fileName, "vsd");
        Globals.ThisAddIn.Application.ActiveDocument.SaveAs(compatibilityFileName);
        Globals.ThisAddIn.Application.ActiveDocument.SaveAs(fileName);

        // delete the compatibility file
        File.Delete(compatibilityFileName);

        MessageBox.Show("完成", "修复");
    }

    #endregion

    #region -- Project Group --

    public void OpenProjectExplorer(Office.IRibbonControl control)
    {
        var document = Globals.ThisAddIn.Application.ActiveDocument;
        var scope = ThisAddIn.ScopeManager.GetScope(document);

        var vm = scope.ServiceProvider.GetRequiredService<ProjectExplorerWindowViewModel>();
        var ui = scope.ServiceProvider.GetRequiredService<IUserInteractionService>();

        ui.Show(vm, new IntPtr(Globals.ThisAddIn.Application.WindowHandle32),
            () => { ThisAddIn.ScopeManager.ReleaseScope(document); });
    }

    public bool CanOpenProjectExplorer(Office.IRibbonControl control)
    {
        return Globals.ThisAddIn.Application.ActiveDocument != null;
    }

    public void ExportElectricalControlSpecification(Office.IRibbonControl control)
    {
        ElectricalControlSpecificationHelper.Generate(Globals.ThisAddIn.Application.ActiveDocument);
    }

    #endregion

    #region -- Ribbon Callbacks --

    //Create callback methods here. For more information about adding callback methods, visit https://go.microsoft.com/fwlink/?LinkID=271226

    public void Ribbon_Load(Office.IRibbonUI ribbonUI)
    {
        _ribbon = ribbonUI;

        // register on triggers to update button status.
        RegisterUpdateForElements();
    }

    // 2025.02.06: 增加一个上次刷新的时间，避免在空闲时频繁刷新。
    private DateTime? _lastInvalidate;

    /// <summary>
    ///     Because the state of the buttons on ribbon will not re-compute once loaded.
    ///     So the re-computation needs to be triggered manually by calling _ribbon.Invalidate().
    ///     As the button state is related to if there is a document in open state, observe on these two events.
    /// </summary>
    private void RegisterUpdateForElements()
    {
        Globals.ThisAddIn.Application.WindowOpened += _ =>
        {
            _ribbon.Invalidate();
            _lastInvalidate = DateTime.Now;
        };
        Globals.ThisAddIn.Application.WindowChanged += _ =>
        {
            _ribbon.Invalidate();
            _lastInvalidate = DateTime.Now;
        };
        Globals.ThisAddIn.Application.VisioIsIdle += _ =>
        {
            if (_lastInvalidate != null && !(DateTime.Now - _lastInvalidate > TimeSpan.FromMinutes(5))) return;

            _ribbon.Invalidate();
            _lastInvalidate = DateTime.Now;
        };
    }

    #endregion

    #region -- Design Group --

    public void LoadLibraries(Office.IRibbonControl control)
    {
        // 2025.02.07: 使用RuntimePath
        var service = ThisAddIn.Services.GetRequiredService<IConfigurationService>();
        LibraryHelper.OpenLibraries(Path.Combine(service.RuntimeConfiguration.DataPath, "libraries"));
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
        var doc = Globals.ThisAddIn.Application.ActiveDocument;

        //remove hidden information to reduce size
        doc.RemoveHiddenInformation((int)VisRemoveHiddenInfoItems.visRHIMasters);

        var service = ThisAddIn.Services.GetRequiredService<IDocumentUpdateService>();

        // 2024.12.9更新：在更新时，一部分用户希望更新所有的模具，但一部分用户希望保留自己修改后的模具，此处弹框要求用户选择哪些模具需要被更新
        var mastersNeedUpdate = service.GetOutdatedMasters(Globals.ThisAddIn.Application.ActiveDocument)
            .Select(x =>
                new DocumentMasterViewModel(x)
                {
                    IsSelected = true
                })
            .ToArray();
        // 2025.02.05： 用户如果点击了取消按钮，则返回null，用户如果点击了确定按钮，则获得待更新的清单
        var ui = ThisAddIn.Services.GetRequiredService<IUserInteractionService>();
        var mastersToUpdate = await ui
            .ShowDialog<ConfirmUpdateDocumentWindowViewModel, VisioMaster[]?>(
                new ConfirmUpdateDocumentWindowViewModel(mastersNeedUpdate), ThisAddIn.GetApplicationHandle());

        // 如果用户取消了，或者待更新清单为空，则取消操作
        if (mastersToUpdate == null || !mastersToUpdate.Any()) return;


        // 文档可能有两种情况：
        // 1. 文档是新建的，此时没有FullName，但是这种情况不应该发生，因为没有检查一个新建的文档，因为该文档随时可被丢弃。
        // 2. 文档曾经被保存过，此时才有检查更新的必要
        // 所以此处假定文档一定有fullname。
        var filePath = doc.FullName;
        doc.Close();

        try
        {
            // do update
            await service.UpdateAsync(filePath, mastersToUpdate);

            // 2025.02.06: 此处增加一个更新成功提示
            MessageBox.Show("更新成功", "文档更新");
        }
        catch (DocumentFailedToUpdateException e)
        {
            MessageBox.Show($"文档更新遇到了些问题，但是很难说是什么问题，请联系李婧雅。错误信息：{e.Message}", "文档更新");
        }
        catch (Exception e)
        {
            MessageBox.Show($"更新失败，{e.Message}", "文档更新");
        }

        // reopen after updated
        Globals.ThisAddIn.Application.Documents.Open(filePath);
    }

    public bool IsUpdateDocumentValid(Office.IRibbonControl control)
    {
        if (Globals.ThisAddIn.Application.ActiveDocument == null) return false;

        // check if the document is the visDrawing, not the stencil or other type
        if (Globals.ThisAddIn.Application.ActiveDocument.Type != VisDocumentTypes.visTypeDrawing) return false;

        // 如果文档从来没有被存储过，则不检查
        if (!Path.IsPathRooted(Globals.ThisAddIn.Application.ActiveDocument.FullName)) return false;

        // check if the AE style exists, if the AE style exists, means this is a target drawing.
        if (Globals.ThisAddIn.Application.ActiveDocument.Styles.OfType<IVStyle>()
                .SingleOrDefault(x => x.Name == StyleDict.Normal) ==
            null) return false;

        LogHost.Default.Info(
            $"Checking if the masters in {Globals.ThisAddIn.Application.ActiveDocument.FullName} is up to date...");

        // check if the version is out of date

        var localMasters = Globals.ThisAddIn.Application.ActiveDocument.Masters.OfType<IVMaster>().Select(x =>
            new MasterSnapshotDto
            {
                BaseId = x.BaseID,
                UniqueId = x.UniqueID,
                Name = x.NameU,
            }); 

        var documentUpdateService = ThisAddIn.Services.GetRequiredService<IDocumentUpdateService>();

        var needUpdate = documentUpdateService.HasUpdate(localMasters);
        LogHost.Default.Info(needUpdate
            ? $"{Globals.ThisAddIn.Application.ActiveDocument.FullName} need update."
            : $"{Globals.ThisAddIn.Application.ActiveDocument.FullName} is up to date.");

        return needUpdate;
    }

    public void OpenTools(Office.IRibbonControl control)
    {
        var document = Globals.ThisAddIn.Application.ActiveDocument;
        var scope = ThisAddIn.ScopeManager.GetScope(document);

        var vm = scope.ServiceProvider.GetRequiredService<ToolsWindowViewModel>();
        var ui = scope.ServiceProvider.GetRequiredService<IUserInteractionService>();

        ui.Show(vm, ThisAddIn.GetApplicationHandle(),
            () => { ThisAddIn.ScopeManager.ReleaseScope(document); });
    }

    #endregion

    #region -- Check Group --

    public void ValidateDesignationUnique(Office.IRibbonControl control)
    {
        ErrorHelper.HighlightShapeWithDuplicatedDesignationWithinGroup(Globals.ThisAddIn.Application.ActivePage);
        _ribbon.Invalidate();
    }

    public void ValidateMasterExist(Office.IRibbonControl control)
    {
        ErrorHelper.HighlightShapeLostMaster(Globals.ThisAddIn.Application.ActivePage);
        _ribbon.Invalidate();
    }

    public void ValidatePipeline(Office.IRibbonControl control)
    {
        ErrorHelper.HighlightPipelineWithFormulaError(Globals.ThisAddIn.Application.ActivePage);
        _ribbon.Invalidate();
    }

    public void ClearValidationMarks(Office.IRibbonControl control)
    {
        ErrorHelper.ClearCheckMarks(Globals.ThisAddIn.Application.ActivePage);
        _ribbon.Invalidate();
    }

    public bool IsCheckValid(Office.IRibbonControl control)
    {
        return Globals.ThisAddIn.Application.ActiveDocument != null && !IsClearValidationMarksValid(control);
    }

    public bool IsClearValidationMarksValid(Office.IRibbonControl control)
    {
        if (Globals.ThisAddIn.Application.ActivePage == null) return false;

        var selection = Globals.ThisAddIn.Application.ActivePage.CreateSelection(VisSelectionTypes.visSelTypeByLayer,
            VisSelectMode.visSelModeSkipSuper, LayerDict.Validation);
        return selection.Count > 0;
    }

    #endregion

    #region -- Material Context Menu --

    public void ShowMaterialDataPane(Office.IRibbonControl control)
    {
        WindowHelper.ShowTaskPane<MaterialPaneView, MaterialPaneViewModel>("物料",
            (shape, vm) =>
            {
                if (shape != null)
                    vm.Code = shape.TryGetValue(CellDict.MaterialCode) ?? string.Empty;
            });
    }

    public void DeleteDesignMaterial(Office.IRibbonControl control)
    {
        foreach (var shape in Globals.ThisAddIn.Application.ActiveWindow.Selection.OfType<Shape>())
        {
            if (!shape.CellExistsN(CellDict.MaterialCode, VisExistsFlags.visExistsLocally)) continue;
            shape.TrySetValue(CellDict.MaterialCode, "");
        }
    }

    public bool HasMaterial(Office.IRibbonControl control)
    {
        var selected = Globals.ThisAddIn.Application.ActiveWindow.Selection.OfType<IVShape>();
        return selected.Any(x =>
            x.CellExistsN(CellDict.MaterialCode, VisExistsFlags.visExistsLocally) &&
            !string.IsNullOrEmpty(x.Cells[CellDict.MaterialCode].ResultStr[VisUnitCodes.visUnitsString]));
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
        return selection != null && selection.Master?.BaseID == BaseIdDict.Frame;
    }

    #endregion

    #region -- Shape Context Menu --

    public string GetToggleTypeLabel(Office.IRibbonControl control)
    {
        var target = Globals.ThisAddIn.Application.ActiveWindow.Selection[1];

        for (short i = 1; i <= target.LayerCount; i++)
        {
            var layer = target.Layer[i];
            if (layer.NameU == LayerDict.Optional) return "设为标准";
        }

        return "设为选配";
    }

    public void ToggleType(Office.IRibbonControl control)
    {
        var target = Globals.ThisAddIn.Application.ActiveWindow.Selection[1];
        FormatHelper.ToggleOptional(target);
    }

    #endregion
}