using AE.PID.Interfaces;

namespace AE.PID.Models.VisProps;

public class ValueProp : Prop, IValueProp
{
    protected ValueProp(string name, string prefix, string value) : base(name, prefix)
    {
        DefaultValue = value;
    }

    public string DefaultValue { get; }
}