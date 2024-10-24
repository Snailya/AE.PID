namespace AE.PID.Visio.Core.Models;

public class ValuePatch(string propertyName, object value, bool createIfNotExists = false)
{
    public string PropertyName { get; set; } = propertyName;
    public object Value { get; set; } = value;
    public bool CreateIfNotExists { get; set; } = createIfNotExists;
}