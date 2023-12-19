using AE.PID.Interfaces;

namespace AE.PID.Models.VisProps;

public class ActionData(string name, string action, string menu, string @checked = "", bool flyoutChild = false)
    : Prop(name,
        "Actions"), IActionData
{
    public string Action { get; set; } = action;
    public string Menu { get; set; } = menu;
    public string Checked { get; set; } = @checked;
    public string FlyoutChild { get; set; } = flyoutChild.ToString().ToUpper();
}