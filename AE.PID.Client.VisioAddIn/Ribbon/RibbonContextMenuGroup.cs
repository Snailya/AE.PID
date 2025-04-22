using System.Collections.Generic;
using System.Linq;
using Microsoft.Office.Core;

namespace AE.PID.Client.VisioAddIn;

internal class RibbonContextMenuGroup(string id, string label, IEnumerable<IRibbonCommand> commands) : IRibbonItem
{
    public string Id { get; } = id;

    public bool GetVisible(IRibbonControl control)
    {
        return commands.Any(x => x.CanExecute(control));
    }

    public string GetLabel(IRibbonControl control)
    {
        return label;
    }
}