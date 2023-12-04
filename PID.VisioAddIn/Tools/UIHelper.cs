using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using Microsoft.Office.Interop.Visio;

namespace AE.PID.Tools;

internal static class UiHelper
{
    /// <summary>
    ///     Create a visio window to hold visual element.
    /// </summary>
    /// <param name="caption"></param>
    /// <param name="content"></param>
    /// <returns></returns>
    public static IntPtr CreateAnchorBarAddonWithUserControl(string caption,
        UserControl content)
    {
        try
        {
            // Create a visio window to hold the visual, this is not necessary as without this we still could create a visual element using HwndSource class. However, to make the visual elment not a standalone window, we need to get the Hwnd of this parent window, and specify the WS_CHILD value for window style.
            var state = VisWindowStates.visWSFloating | VisWindowStates.visWSVisible;
            var types = VisWinTypes.visAnchorBarAddon;
            // Notice that the nWidth and nHeight parameters are not necessary. If they are specified as 0, it will decide by the system. 
            var parent =
                Globals.ThisAddIn.Application.ActiveWindow.Windows.Add(caption, state, types, 8, 10, 0, 0, 0, 0, 0);

            var parameters = new HwndSourceParameters(caption)
            {
                ParentWindow = (IntPtr)parent.WindowHandle32,
                // Notice if WS_CHILD is missing, the visual will not display as a child of the parent window.
                WindowStyle = NativeMethods.WS_VISIBLE | NativeMethods.WS_CHILD
            };
            parameters.SetSize((int)content.Width, (int)content.Height);


            // Make sure we set the parent successfully
            Debug.Assert(parameters.ParentWindow == (IntPtr)parent.WindowHandle32);

            var source = new HwndSource(parameters)
            {
                RootVisual = content,
                SizeToContent = SizeToContent.WidthAndHeight
            };

            source.ContentRendered += (sender, args) =>
            {
                if (sender is HwndSource hwndSource)
                {
                }
            };

            return source.Handle;
        }
        catch (Exception)
        {
            Debugger.Break();
            throw;
        }
    }
}