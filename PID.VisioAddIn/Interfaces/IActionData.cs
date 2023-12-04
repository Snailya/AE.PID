namespace AE.PID.Interfaces;

public interface IActionData : IProp
{
    /// <summary>
    ///     The formula to be executed when a user chooses a command on a shortcut or action tag menu.
    /// </summary>
    public string Action { get; set; }

    /// <summary>
    ///     Defines the name of a menu item that appears on a shortcut or action tag menu for a shape or page.
    /// </summary>
    public string Menu { get; set; }

    /// <summary>
    ///     The formula that indicates whether an item is checked on the shortcut or action tag menu.
    /// </summary>
    public string Checked { get; set; }

    /// <summary>
    ///     Determines whether the row is a child flyout menu of the last row above it that is not a flyout child.
    /// </summary>
    public string FlyoutChild { get; set; }
}