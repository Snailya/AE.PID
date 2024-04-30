using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reactive.Subjects;
using System.Reflection;
using System.Runtime.InteropServices;
using AE.PID.Controllers.Services;
using AE.PID.Models.BOM;
using AE.PID.Properties;
using AE.PID.Tools;
using AE.PID.Views;
using AE.PID.Views.Pages;
using AE.PID.Views.Windows;
using Microsoft.Office.Core;
using Microsoft.Office.Interop.Visio;
using NLog;
using ReactiveUI;
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
public class Ribbon : IRibbonExtensibility
{
    private Dictionary<string, Bitmap> _buttonImages = new();
    private Subject<Command> _commandInvoker = new();
    private Logger _logger = LogManager.GetCurrentClassLogger();
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

        _commandInvoker.Subscribe(command =>
            {
                _logger.Info($"{command} [Init by User]");

                switch (command)
                {
                    case Command.LoadLibrary:
                        VisioHelper.OpenLibraries();
                        break;
                    case Command.FormatDocument:
                        VisioHelper.FormatDocument(Globals.ThisAddIn.Application.ActiveDocument);
                        break;
                    case Command.UpdateDocument:
                        VisioHelper.UpdateDocumentStencil(Globals.ThisAddIn.Application.ActiveDocument);
                        break;
                    case Command.InsertLegend:
                        new LegendGenerator(Globals.ThisAddIn.Application.ActivePage).Insert();
                        break;
                    case Command.OpenSelectTool:
                        Globals.ThisAddIn.WindowManager.Show(new ShapeSelectionPage());
                        break;
                    case Command.OpenExportTool:
                        Globals.ThisAddIn.WindowManager.Show(new BomPage());
                        break;
                    case Command.OpenSettings:
                        Globals.ThisAddIn.WindowManager.Show(new UserSettingsPage());
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(command), command, null);
                }
            },
            error => { }, () => { });
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

    #region Context Menus

    public void SelectDesignMaterial(IRibbonControl control)
    {
        throw new NotImplementedException();
    }
    
    public void DeleteDesignMaterial(IRibbonControl control)
    {
        foreach (var shape in Globals.ThisAddIn.Application.ActiveWindow.Selection.OfType<Shape>())
            if (shape.HasCategory("Equipment"))
            {
                using var equipment = new Equipment(shape);
                equipment.DesignMaterial = null;
            }
            else if (shape.HasCategory("FunctionalElement"))
            {
                using var functionalElement = new FunctionalElement(shape);
                functionalElement.DesignMaterial = null;
            }
    }

    public bool IsMaterialButtonVisible(IRibbonControl control)
    {
        var window = Globals.ThisAddIn.Application.ActiveWindow;
        return window.Document.Type == VisDocumentTypes.visTypeDrawing && window.Selection.OfType<IVShape>()
            .All(x => x.HasCategory("Equipment") || x.HasCategory("Instrument"));
    }

    #endregion

    private enum Command
    {
        LoadLibrary,
        FormatDocument,
        UpdateDocument,
        InsertLegend,
        OpenSelectTool,
        OpenExportTool,
        OpenSettings
    }

    #region Ribbon

    public Bitmap GetButtonImage(IRibbonControl control)
    {
        var buttonId = control.Id;
        if (_buttonImages.TryGetValue(buttonId, out var image)) return image;

        _buttonImages[buttonId] = ((Icon)Resources.ResourceManager.GetObject(buttonId))?.ToBitmap();
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
        _commandInvoker.OnNext(Command.OpenExportTool);
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
    }

    public void Debug(IRibbonControl control)
    {

    }

    #endregion
}