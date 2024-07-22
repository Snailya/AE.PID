using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reactive.Subjects;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using AE.PID.Models;
using AE.PID.Properties;
using AE.PID.Services;
using AE.PID.Tools;
using AE.PID.Views;
using Microsoft.Office.Core;
using Microsoft.Office.Interop.Visio;
using Splat;
using Shape = Microsoft.Office.Interop.Visio.Shape;

// TODO:   按照以下步骤启用功能区(XML)项:

// 1. 将以下代码块复制到 ThisAddin、ThisWorkbook 或 ThisDocument 类中。
//  protected override Microsoft.Office.Core.IRibbonExtensibility CreateRibbonExtensibilityObject()
//  {
//      return new Ribbon1();
//  }

// 2. 在此类的“功能区回调”区域中创建回调方法，以处理用户
//    操作(如单击某个按钮)。注意: 如果已经从功能区设计器中导出此功能区，
//    则将事件处理程序中的代码移动到回调方法并修改该代码以用于
//    功能区扩展性(RibbonX)编程模型。

// 3. 向功能区 XML 文件中的控制标记分配特性，以标识代码中的相应回调方法。  

// 有关详细信息，请参见 Visual Studio Tools for Office 帮助中的功能区 XML 文档。


namespace AE.PID;

[ComVisible(true)]
public class Ribbon : IRibbonExtensibility, IEnableLogger
{
    private readonly Subject<Command> _commandInvoker = new();
    private Dictionary<string, Bitmap> _buttonImages = [];
    private IRibbonUI _ribbon;

    #region IRibbonExtensibility 成员

    public string GetCustomUI(string ribbonId)
    {
        return GetResourceText("AE.PID.Ribbon.xml");
    }

    #endregion

    public void Ribbon_Load(IRibbonUI ribbonUi)
    {
        _ribbon = ribbonUi;

        // register on triggers to update button status.
        RegisterUpdateForElements();

        // distribute the task based on the command 
        _commandInvoker.Subscribe(command =>
            {
                this.Log().Info($"{command} [Init by User]");

                switch (command)
                {
                    case Command.LoadLibrary:
                        var libraryPaths = Locator.Current.GetService<ConfigurationService>()!.Libraries.Items
                            .Select(x => x.Path).ToList();
                        VisioHelper.OpenLibraries(libraryPaths);
                        break;
                    case Command.FormatDocument:
                        VisioHelper.FormatDocument(Globals.ThisAddIn.Application.ActiveDocument);
                        break;
                    case Command.UpdateDocument:
                        // VisioHelper.UpdateDocument(Globals.ThisAddIn.Application.ActiveDocument);
                        var monitor = Locator.Current.GetService<DocumentMonitor>()!;
                        _ = monitor.UseServerSideUpdate(Globals.ThisAddIn.Application.ActiveDocument);
                        break;
                    case Command.InsertLegend:
                        VisioHelper.InsertLegend(Globals.ThisAddIn.Application.ActivePage);
                        break;
                    case Command.OpenSelectTool:
                        WindowManager.Dispatcher!.Invoke(() =>
                        {
                            WindowManager.GetInstance()!.Show(new SelectToolPage());
                        });
                        break;
                    case Command.OpenProjectExplorer:
                        WindowManager.Dispatcher!.Invoke(() =>
                        {
                            WindowManager.GetInstance()!.Show(new ProjectExplorerPage(),
                                new MaterialsSelectionPage());
                        });
                        break;
                    case Command.OpenSettings:
                        WindowManager.Dispatcher!.Invoke(() =>
                        {
                            WindowManager.GetInstance()!.Show(new SettingsPage());
                        });
                        break;
                    case Command.Help:
                        Process.Start("https://snailya.github.io/posts/ae-pid%E5%BF%AB%E9%80%9F%E5%85%A5%E9%97%A8/");
                        break;
                    case Command.ValidateDesignationUnique:
                        VisioHelper.CheckDesignationUnique(Globals.ThisAddIn.Application.ActivePage);
                        break;
                    case Command.ClearValidationMarks:
                        VisioHelper.ClearCheckMarks(Globals.ThisAddIn.Application.ActivePage);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(command), command, null);
                }

                _ribbon.Invalidate();
            },
            error => { this.Log().Error(error); },
            () => { });
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


    #region 帮助器

    private static string GetResourceText(string resourceName)
    {
        var asm = Assembly.GetExecutingAssembly();
        var resourceNames = asm.GetManifestResourceNames();
        foreach (var t in resourceNames)
        {
            if (string.Compare(resourceName, t, StringComparison.OrdinalIgnoreCase) != 0) continue;
            using var resourceReader = new StreamReader(asm.GetManifestResourceStream(t));
            return resourceReader.ReadToEnd();
        }

        return null;
    }

    #endregion

    private enum Command
    {
        LoadLibrary,
        FormatDocument,
        UpdateDocument,
        InsertLegend,
        OpenSelectTool,
        ValidateDesignationUnique,
        ClearValidationMarks,
        OpenProjectExplorer,
        OpenSettings,
        Help
    }

    #region Context Menus

    public void SelectDesignMaterial(IRibbonControl control)
    {
        var selected = Globals.ThisAddIn.Application.ActiveWindow.Selection.OfType<Shape>().Single();
        PartItem? element = selected.HasCategory("Equipment") ? new Equipment(selected) :
            selected.HasCategory("FunctionalElement") ? new FunctionalElement(selected) : null;
        if (element == null)
            return;

        WindowManager.Dispatcher!.InvokeAsync(() =>
        {
            var page = new MaterialsSelectionPage();
            page.ViewModel!.Element = element;
            WindowManager.GetInstance()!.Show(page);
        });
    }

    public void DeleteDesignMaterial(IRibbonControl control)
    {
        foreach (var shape in Globals.ThisAddIn.Application.ActiveWindow.Selection.OfType<Shape>())
        {
            if (!shape.CellExistsN("Prop.D_BOM", VisExistsFlags.visExistsLocally)) continue;
            VisioHelper.DeleteDesignMaterial(shape);
        }
    }

    public bool IsPartItem(IRibbonControl control)
    {
        var window = Globals.ThisAddIn.Application.ActiveWindow;
        return window.Document.Type == VisDocumentTypes.visTypeDrawing && window.Selection.OfType<IVShape>()
            .All(x => x.HasCategory("Equipment") || x.HasCategory("Instrument") || x.HasCategory("FunctionalElement"));
    }

    public bool HasDesignMaterial(IRibbonControl control)
    {
        var selected = Globals.ThisAddIn.Application.ActiveWindow.Selection.OfType<IVShape>();
        return selected.Any(x =>
            x.CellExistsN("Prop.D_BOM", VisExistsFlags.visExistsLocally) &&
            !string.IsNullOrEmpty(x.Cells["Prop.D_BOM"].ResultStr[VisUnitCodes.visUnitsString]));
    }

    public void InsertFunctionalElement(IRibbonControl control)
    {
        var target = Globals.ThisAddIn.Application.ActiveWindow.Selection[1];
        VisioHelper.InsertFunctionalElement(target);
    }

    public bool IsSingleEquipment(IRibbonControl control)
    {
        var window = Globals.ThisAddIn.Application.ActiveWindow;
        // if not the drawing page
        if (window.Document.Type != VisDocumentTypes.visTypeDrawing) return false;
        // if multiple selection
        if (window.Selection.Count != 1) return false;
        // if not pure Equipment
        return window.Selection.OfType<IVShape>()
            .All(x => x.HasCategory("Equipment") && !x.HasCategory("Proxy"));
    }

    #endregion

    #region Ribbon

    public Bitmap GetButtonImage(IRibbonControl control)
    {
        var buttonId = control.Id;
        if (_buttonImages.TryGetValue(buttonId, out var image)) return image;

        _buttonImages[buttonId] = ((Icon)Resources.ResourceManager.GetObject("ICON_" + buttonId)).ToBitmap();
        return _buttonImages[buttonId];
    }

    public void LoadLibraries(IRibbonControl control)
    {
        _commandInvoker.OnNext(Command.LoadLibrary);
    }

    public void FormatPage(IRibbonControl control)
    {
        _commandInvoker.OnNext(Command.FormatDocument);
    }

    public void OpenSelectTool(IRibbonControl control)
    {
        _commandInvoker.OnNext(Command.OpenSelectTool);
    }

    public void UpdateDocument(IRibbonControl control)
    {
        _commandInvoker.OnNext(Command.UpdateDocument);
    }

    public void OpenExportTool(IRibbonControl control)
    {
        _commandInvoker.OnNext(Command.OpenProjectExplorer);
    }

    public void EditSettings(IRibbonControl control)
    {
        _commandInvoker.OnNext(Command.OpenSettings);
    }

    public void InsertLegend(IRibbonControl control)
    {
        _commandInvoker.OnNext(Command.InsertLegend);
    }

    public void Help(IRibbonControl control)
    {
        _commandInvoker.OnNext(Command.Help);
    }

    public async Task Debug(IRibbonControl control)
    {
    }

    public bool IsDocumentOpened(IRibbonControl control)
    {
        return Globals.ThisAddIn.Application.ActiveDocument != null;
    }

    public bool IsDocumentOutOfDate(IRibbonControl control)
    {
        if (Globals.ThisAddIn.Application.ActiveDocument == null) return false;

        var monitor = Locator.Current.GetService<DocumentMonitor>()!;
        return monitor.IsMasterOutOfDate(Globals.ThisAddIn.Application
            .ActiveDocument);
    }

    public void ValidateDesignationUnique(IRibbonControl control)
    {
        _commandInvoker.OnNext(Command.ValidateDesignationUnique);
    }

    public void ClearValidationMarks(IRibbonControl control)
    {
        _commandInvoker.OnNext(Command.ClearValidationMarks);
    }

    public bool CanValidateDesignationUnique(IRibbonControl control)
    {
        return IsDocumentOpened(control) && !HasValidationMarks(control);
    }

    public bool HasValidationMarks(IRibbonControl control)
    {
        if (Globals.ThisAddIn.Application.ActivePage == null) return false;

        var selection = Globals.ThisAddIn.Application.ActivePage.CreateSelection(VisSelectionTypes.visSelTypeByLayer,
            VisSelectMode.visSelModeSkipSuper, Constants.ValidationLayerName);
        return selection.Count > 0;
    }

    #endregion
}