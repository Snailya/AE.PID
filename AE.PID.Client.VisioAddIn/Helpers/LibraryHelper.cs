using System;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using Microsoft.Office.Interop.Visio;
using Splat;

namespace AE.PID.Client.VisioAddIn;

public abstract class LibraryHelper
{
    public static void OpenLibraries(string path)
    {
        var files = Directory.GetFiles(path).Where(x => x.EndsWith("vssx")).ToArray();

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