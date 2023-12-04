using System.Runtime.InteropServices;

namespace AE.PID.Tools;

/// <summary>
///     Explicitly declare calls to unmanaged code inside a
///     'NativeMethods' class.  This class does not suppress stack walks for
///     unmanaged code permission.
/// </summary>
public class NativeMethods
{
    /// <summary>Windows constant - Sets a new window style.</summary>
    internal const short GWL_STYLE = -16;

    /// <summary>Windows constant - Creates a child window..</summary>
    internal const int WS_CHILD = 0x40000000;

    /// <summary>
    ///     Windows constant - Creates a window that is initially
    ///     visible.
    /// </summary>
    internal const int WS_VISIBLE = 0x10000000;

    /// <summary>
    ///     Declare a private constructor to prevent new instances
    ///     of the NativeMethods class from being created. This constructor
    ///     is intentionally left blank.
    /// </summary>
    private NativeMethods()
    {
        // No initialization is required.
    }

    /// <summary>Prototype of SetParent() for PInvoke</summary>
    [DllImport("user32.dll")]
    internal static extern int SetParent(int hWndChild,
        int hWndNewParent);

    /// <summary>Prototype of SetWindowLong() for PInvoke</summary>
    [DllImport("user32.dll", SetLastError = true)]
    internal static extern int SetWindowLongW(int hwnd,
        int nIndex,
        int dwNewLong);
}