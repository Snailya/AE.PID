using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using AE.PID.Views;
using Microsoft.Office.Interop.Visio;
using Application = Microsoft.Office.Interop.Visio.Application;

namespace AE.PID.Tools;

public abstract class AnchorBarsUsage
{
    /// <summary>
    ///     GUID that identifies the custom anchor window when it
    ///     is merged.
    /// </summary>
    private const string CustomMergeId =
        "{6D5829C6-57E6-4BC7-8451-A27C936AEBC2}";

    /// <summary>This constructor is intentionally left blank.</summary>
    protected AnchorBarsUsage()
    {
        // No initialization is required.
    }

    public static bool ShowAnchorBar(Application visioApplication)
    {
        if (visioApplication == null) return false;

        const string anchorBarTitle = "AE PID 工具箱";
        const string anchorBarMergeTitle = "PID";

        try
        {
            // The anchor bar will be docked to the right of the app window if using visWSCockedRight.
            // If visWSAnchorRight is used, it will appear at the right side inside the drawing window.
            object windowStates = VisWindowStates.visWSDockedRight | VisWindowStates.visWSVisible;

            // The anchor bar is a window centered by an add-on
            object windowTypes = VisWinTypes.visAnchorBarAddon;

            // Add a custom anchor bar window
            var anchorWindow = AddAnchorWindow(visioApplication, anchorBarTitle, windowStates, windowTypes);

            // set contents
            var content = new HostForm();

            AddFormToAnchorWindow(anchorWindow, content);

            // Set MergeId allows the anchor bar window to be identified when it is merged with another window.
            anchorWindow.MergeID = CustomMergeId;

            // Allow the anchor window to be merged with other windows that have a zero-length MergeClass property value
            anchorWindow.MergeClass = "";

            // Set the MergeCaption property with string that is shorter 
            // than the window caption. The MergeCaption property value 
            // appears on the tab of the merged window.
            anchorWindow.MergeCaption = anchorBarMergeTitle;
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex.Message);
            throw;
        }

        return false;
    }

    private static void AddFormToAnchorWindow(Window anchorWindow, Form content)
    {
        try
        {
            // Show the contents as a modeless dialog
            content.Show();

            // Get the window handle of the form
            var windowHandle = content.Handle.ToInt32();

            // Set the form as a visible child window
            if (NativeMethods.SetWindowLongW(windowHandle, NativeMethods.GWL_STYLE,
                    NativeMethods.WS_CHILD | NativeMethods.WS_VISIBLE) == 0 &&
                Marshal.GetLastWin32Error() != 0) throw new Exception("Can not SetWindowLongW");

            // Set the anchor bar window as the parent of the form 
            if (NativeMethods.SetParent(windowHandle, anchorWindow.WindowHandle32) == 0)
                throw new Exception("Can not set parent");

            // Set the dock property of the form to fill, so that the form automatically resizes to the size of the anchor bar
            content.Dock = DockStyle.Fill;

            // Resize the anchor window so it will refresh
            anchorWindow.GetWindowRect(out var left, out var top, out var width, out var height);
            anchorWindow.SetWindowRect(left, top, width, height);
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex.Message);
            throw;
        }
    }

    private static Window AddAnchorWindow(Application visioApplication, string caption, object windowStates,
        object windowTypes)
    {
        Window anchorWindow;

        try
        {
            var left = 8;
            var top = 8;
            var width = 200;
            var height = 400;

            // Add a new anchor bar with the required information
            anchorWindow = visioApplication.ActiveWindow.Windows.Add(
                caption, windowStates, windowTypes, left, top, width, height);
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex.Message);
            throw;
        }

        return anchorWindow;
    }
}