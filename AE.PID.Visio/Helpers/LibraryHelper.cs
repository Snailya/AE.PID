using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using Microsoft.Office.Interop.Visio;
using Splat;

namespace AE.PID.Visio.Helpers;

public abstract class LibraryHelper
{
    private static readonly string Path = System.IO.Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location).ProductName, "Libraries");

    public static void OpenLibraries()
    {
        var files = Directory.GetFiles(Path).Where(x => x.EndsWith("vssx")).ToArray();

        try
        {
            foreach (var file in files)
                Globals.ThisAddIn.Application.Documents.OpenEx(file, (short)VisOpenSaveArgs.visOpenDocked);

            LogHost.Default.Info($"Loaded {files.Length} libraries.");
        }
        catch (Exception ex)
        {
            LogHost.Default.Error(ex, "Failed to load libraries.");

            // display error message
            MessageBox.Show(ex.Message, "加载库失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}