namespace AE.PID.Interfaces;

public interface IUserData : IValueProp
{
    /// <summary>
    ///     Descriptive or instructional text that appears as a tip when the mouse is paused over a value in the Shape Data
    ///     window。
    /// </summary>
    public string Prompt { get; set; }
}