namespace AE.PID.Client.UI.Avalonia;

public class ByGroupViewModel
{
    /// <summary>
    ///     The user-friendly label of the group
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    ///     The property name used for building up expression-tree when perform by group operation
    /// </summary>
    public string PropertyName { get; set; } = string.Empty;
}