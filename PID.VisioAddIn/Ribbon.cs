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
    private IRibbonUI ribbon;
    private Logger _logger;
    private Dictionary<string, Bitmap> buttonImages = new();

    public Ribbon()
    {
        _logger = LogManager.GetCurrentClassLogger();
    }

    #region IRibbonExtensibility 成员

    public string GetCustomUI(string ribbonId)
    {
        return GetResourceText("AE.PID.Ribbon.xml");
    }

    #endregion

    #region 功能区回调

    //在此处创建回叫方法。有关添加回叫方法的详细信息，请访问 https://go.microsoft.com/fwlink/?LinkID=271226

    public void Ribbon_Load(IRibbonUI ribbonUi)
    {
        ribbon = ribbonUi;
    }

    public Bitmap GetButtonImage(IRibbonControl control)
    {
        var buttonId = control.Id;
        if (buttonImages.TryGetValue(buttonId, out var image)) return image;

        buttonImages[buttonId] = ((Icon)Properties.Resources.ResourceManager.GetObject(buttonId))?.ToBitmap();
        return buttonImages[buttonId];
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
        DocumentExporter.Invoke(Globals.ThisAddIn.Application.ActivePage);
    }

    public void EditSettings(IRibbonControl control)
    {
        ConfigurationUpdater.Invoke();
    }

    public void btnToolbox_Click(IRibbonControl control)
    {
        AnchorBarsUsage.ShowAnchorBar(Globals.ThisAddIn.Application);
    }

    #region Context Menus

    public void AddLinkedControl(IRibbonControl control)
    {
        LinkedControlManager.AddControl(Globals.ThisAddIn.Application.ActiveWindow.Selection[1]);
    }

    public bool CanAddLinkedControl(IRibbonControl control)
    {
        return LinkedControlManager.CanAddControl(Globals.ThisAddIn.Application.ActiveWindow.Selection);
    }

    public void Highlight(IRibbonControl control)
    {
        LinkedControlManager.Highlight(Globals.ThisAddIn.Application.ActiveWindow.Selection[1]);
    }

    public bool CanHighlight(IRibbonControl control)
    {
        return LinkedControlManager.CanHighlight(Globals.ThisAddIn.Application.ActiveWindow.Selection);
    }

    #endregion

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