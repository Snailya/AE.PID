using AE.PID.Client.Core;

namespace AE.PID.Client.Infrastructure;

public class PropertyPatch(
    ICompoundKey? target,
    string name,
    object value,
    bool createIfNotExists = false,
    string? label = null)
    : INameValuePair
{
    public bool CreateIfNotExists { get; set; } = createIfNotExists;
    public ICompoundKey? Target { get; set; } = target;

    public string? LabelFormula { get; set; } = label;
    public string Name { get; set; } = name;
    public object Value { get; set; } = value;
}