using System;
using System.Runtime.InteropServices;

namespace AE.PID.Tools;

public static class Win32Ext
{
    // 导入 Win32 API 函数
    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

    // 定义矩形结构体
    [StructLayout(LayoutKind.Sequential)]
    public struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }
}