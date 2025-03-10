namespace AE.PID.Client.Core.VisioExt.Control;

[AttributeUsage(AttributeTargets.Property)]
public class Callout(string baseId) : Attribute
{
    public string BaseId { get; } = baseId;
}