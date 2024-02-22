using AE.PID.Controllers.Services;
using AE.PID.Tools;
using Microsoft.Office.Core;
using Microsoft.Office.Interop.Visio;
using Microsoft.Office.Tools.Ribbon;
using NLog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using AE.PID.Models.Exceptions;
using Office = Microsoft.Office.Core;
using System.Globalization;
using System.Resources;
using System.Windows.Controls;
using AE.PID.Models.VisProps;
using System.Windows;
using System.Drawing.Imaging;
using System.Windows.Media.Imaging;
using System.Windows.Media;
using Brushes = System.Windows.Media.Brushes;
using Point = System.Windows.Point;
using System.Windows.Forms;
using AE.PID.Controllers.Services;

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
    private IRibbonUI _ribbon;
    private Logger _logger = LogManager.GetCurrentClassLogger();
    private Dictionary<string, Bitmap> _buttonImages = new();

    #region IRibbonExtensibility 成员

    public string GetCustomUI(string ribbonId)
    {
        return GetResourceText("AE.PID.Ribbon.xml");
    }

    #endregion

    public void Ribbon_Load(IRibbonUI ribbonUi)
    {
        _ribbon = ribbonUi;
    }

    #region Ribbon

    public Bitmap GetButtonImage(IRibbonControl control)
    {
        var buttonId = control.Id;
        if (_buttonImages.TryGetValue(buttonId, out var image)) return image;

        _buttonImages[buttonId] = ((Icon)Properties.Resources.ResourceManager.GetObject(buttonId))?.ToBitmap();
        return _buttonImages[buttonId];
    }

    public void LoadLibraries(IRibbonControl control)
    {
        try
        {
            foreach (var path in Globals.ThisAddIn.Configuration.LibraryConfiguration.Libraries.Select(x => x.Path))
                Globals.ThisAddIn.Application.Documents.OpenEx(path,
                    (short)VisOpenSaveArgs.visOpenDocked);

            _logger.Info($"Opened {Globals.ThisAddIn.Configuration.LibraryConfiguration.Libraries.Count} libraries.");
        }
        catch (Exception ex)
        {
            _logger.Error(ex);
        }
    }

    public void FormatPage(IRibbonControl control)
    {
        DocumentInitializer.Invoke(Globals.ThisAddIn.Application.ActiveDocument);
    }

    public void OpenSelectTool(IRibbonControl control)
    {
        ShapeSelector.Invoke(Globals.ThisAddIn.Application.ActivePage);
    }

    public void UpdateDocument(IRibbonControl control)
    {
        DocumentUpdater.Invoke(Globals.ThisAddIn.Application.ActiveDocument);
    }

    public void OpenExportTool(IRibbonControl control)
    {
        DocumentExporter.Invoke();
    }

    public void EditSettings(IRibbonControl control)
    {
        ConfigurationUpdater.Invoke();
    }

    public void btnToolbox_Click(IRibbonControl control)
    {
        AnchorBarsUsage.ShowAnchorBar(Globals.ThisAddIn.Application);
    }

    public void InsertLegend(IRibbonControl control)
    {
        LegendService.Invoke(Globals.ThisAddIn.Application.ActivePage);
    }

    public void Help(IRibbonControl control)
    {
    }

    #endregion

    #region Commands

    public void CopyOverride(IRibbonControl control, ref bool cancel)
    {
        Globals.ThisAddIn.Application.ActiveWindow.Selection.GetIDs(out var ids);
        LinkedControlManager.PreviousCopy = ids.OfType<int>().ToList();

        cancel = false;
    }

    #endregion

    #region Context Menus

    public void InsertLinked(IRibbonControl control)
    {
        LinkedControlManager.InsertFunctionalElement(Globals.ThisAddIn.Application.ActiveWindow.Selection[1]);
    }

    public bool CanInsert(IRibbonControl control)
    {
        return LinkedControlManager.CanInsert(Globals.ThisAddIn.Application.ActiveWindow.Selection);
    }

    public void HighlightPrimary(IRibbonControl control)
    {
        LinkedControlManager.HighlightPrimary(Globals.ThisAddIn.Application.ActiveWindow.Selection[1]);
    }

    public bool CanHighlightPrimary(IRibbonControl control)
    {
        return LinkedControlManager.CanHighlightPrimary(Globals.ThisAddIn.Application.ActiveWindow.Selection);
    }

    public void HighlightLinked(IRibbonControl control)
    {
        LinkedControlManager.HighlightLinked(Globals.ThisAddIn.Application.ActiveWindow.Selection[1]);
    }

    public bool CanHighlightLinked(IRibbonControl control)
    {
        return LinkedControlManager.CanHighlightLinked(Globals.ThisAddIn.Application.ActiveWindow.Selection);
    }

    public void PasteWithLinked(IRibbonControl control)
    {
        LinkedControlManager.PasteToLocation();
    }

    public bool CanPaste(IRibbonControl control)
    {
        return LinkedControlManager.CanPaste();
    }

    #endregion

    #region 帮助器

    private static string GetResourceText(string resourceName)
    {
        var asm = Assembly.GetExecutingAssembly();
        var resourceNames = asm.GetManifestResourceNames();
        for (var index = 0; index < resourceNames.Length; index++)
        {
            var t = resourceNames[index];
            if (string.Compare(resourceName, t, StringComparison.OrdinalIgnoreCase) != 0) continue;
            using var resourceReader = new StreamReader(asm.GetManifestResourceStream(t));
            return resourceReader.ReadToEnd();
        }

        return null;
    }

    #endregion
}