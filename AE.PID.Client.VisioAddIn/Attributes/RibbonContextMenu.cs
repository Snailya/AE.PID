using System;

namespace AE.PID.Client.VisioAddIn;

[AttributeUsage(AttributeTargets.Class, Inherited = false)]
internal class RibbonContextMenu(string id, string label) : Attribute
{
    public string Id { get; } = id;
    public string Label { get; } = label;
}