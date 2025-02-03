using System;
using System.Windows.Forms;
using AE.PID.Core.Models;
using Splat;
using Shape = Microsoft.Office.Interop.Visio.Shape;

namespace AE.PID.Client.VisioAddIn;

public abstract class ProxyHelper
{
    public static void Insert(Shape target, FunctionType type)
    {
        var baseId = type switch
        {
            FunctionType.FunctionElement => "{B28A5C75-E7CB-4700-A060-1A6D0A777A94}",
            FunctionType.Equipment => "{07190C20-ED3D-4585-B38A-0AC24FAB2974}",
            FunctionType.FunctionGroup => "{B6BD7D33-085C-442E-8404-6CF8D6CE30B0}",
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
        };

        var undoScope = target.Application.BeginUndoScope("插入代理");
        try
        {
            // get the position of target
            var position = target.GetPinLocation();

            // get the object of functional element
            // todo: fallback document path
            var master = target.Document.GetMaster(baseId, "");
            var proxy = target.ContainingPage.DropMetric(master, position);
            proxy.CalloutTarget = target;

            target.Application.EndUndoScope(undoScope, true);
        }
        catch (Exception ex)
        {
            target.Application.EndUndoScope(undoScope, false);

            // log
            LogHost.Default.Error(ex, "Failed to insert proxy");

            // display error message
            MessageBox.Show(ex.Message, "插入代理失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}