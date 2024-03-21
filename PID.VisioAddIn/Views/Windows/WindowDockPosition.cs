namespace AE.PID.Views.Windows;

public enum WindowDockPosition
{
    /// <summary>
    ///     Not docked
    /// </summary>
    Undocked = 0,

    /// <summary>
    ///     Docked to the left of the screen
    /// </summary>
    Left = 1,

    /// <summary>
    ///     Docked to the right of the screen
    /// </summary>
    Right = 2,

    /// <summary>
    ///     Docked to the top/bottom of the screen
    /// </summary>
    TopBottom = 3,

    /// <summary>
    ///     Docked to the top-left of the screen
    /// </summary>
    TopLeft = 4,

    /// <summary>
    ///     Docked to the top-right of the screen
    /// </summary>
    TopRight = 5,

    /// <summary>
    ///     Docked to the bottom-left of the screen
    /// </summary>
    BottomLeft = 6,

    /// <summary>
    ///     Docked to the bottom-right of the screen
    /// </summary>
    BottomRight = 7
}