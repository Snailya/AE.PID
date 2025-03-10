namespace AE.PID.Client.Core.VisioExt.Control;

[AttributeUsage(AttributeTargets.Property)]
public class Connected(string[]? includes = null, string[]? excepts = null) : Attribute
{
    public string[]? Includes { get; set; } = includes;
    public string[]? Excepts { get; } = excepts;
}