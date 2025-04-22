using System;
using System.Collections.Concurrent;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using Splat;
using Office = Microsoft.Office.Core;

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
    private RibbonCommandManager _commandManager = new();

    // 2025.02.06: 增加一个上次刷新的时间，避免在空闲时频繁刷新。
    private ConcurrentDictionary<string, DateTime> _lastInvalidates = new();

    private Office.IRibbonUI _ribbon;

    #region IRibbonExtensibility Members

    public string GetCustomUI(string ribbonID)
    {
        return GetResourceText("AE.PID.Client.VisioAddIn.Ribbon.xml");
    }

    #endregion

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

    public void OnAction(Office.IRibbonControl control)
    {
        if (_commandManager[control.Id] is IRibbonCommand ribbonCommand)
            ribbonCommand.Execute(control);

        _ribbon.Invalidate();
    }

    public bool GetEnabled(Office.IRibbonControl control)
    {
        if (_commandManager[control.Id] is IRibbonCommand ribbonCommand)
            return ribbonCommand.CanExecute(control);

        return false;
    }

    public bool GetVisible(Office.IRibbonControl control)
    {
        if (_commandManager[control.Id] is { } ribbonItem)
            return ribbonItem.GetVisible(control);

        return false;
    }

    public string GetLabel(Office.IRibbonControl control)
    {
        if (_commandManager[control.Id] is { } ribbonItem)
            return ribbonItem.GetLabel(control);

        return control.Id;
    }

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
        Globals.ThisAddIn.Application.VisioIsIdle += app =>
        {
            var document = app.ActiveDocument?.FullName;
            if (document == null) return;

            if (_lastInvalidates.TryGetValue(document, out var lastInvalidate) &&
                lastInvalidate + TimeSpan.FromMinutes(5) > DateTime.Now) return;

            _ribbon.Invalidate();
            _lastInvalidates.TryAdd(document, DateTime.Now);
            LogHost.Default.Info($"Invalidate ribbon for {document}");
        };

        Globals.ThisAddIn.Application.BeforeDocumentClose += doc =>
        {
            _lastInvalidates.TryRemove(doc.FullName, out _);
        };
    }

#if DEBUG
    public async void Debug(Office.IRibbonControl control)
    {
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
#endif
}