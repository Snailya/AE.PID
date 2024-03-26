using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;

namespace AE.PID.Views.Windows;

public class BaseWindowViewModel : INotifyPropertyChanged
{
    #region Constructor

    /// <summary>
    ///     Default constructor
    /// </summary>
    public BaseWindowViewModel(Window window)
    {
        _mWindow = window;

        // Listen out for the window resizing
        _mWindow.StateChanged += (sender, e) =>
        {
            // Fire off events for all properties that are affected by a resize
            WindowResized();
        };

        // Fix window resize issue
        _mWindowResizer = new WindowResizer(_mWindow);

        // Listen out for dock changes
        _mWindowResizer.WindowDockChanged += dock =>
        {
            // Store last position
            _mDockPosition = dock;

            // Fire off resize events
            WindowResized();
        };
    }

    #endregion

    #region Event

    public event PropertyChangedEventHandler PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    #endregion

    #region Private Member

    /// <summary>
    ///     The window this view partModel controls.
    /// </summary>
    protected readonly Window _mWindow;

    /// <summary>
    ///     The window resizer helper that keeps the window size correct in various states
    /// </summary>
    private readonly WindowResizer _mWindowResizer;

    /// <summary>
    ///     The margin around the window to allow for a drop shadow
    /// </summary>
    private int _mOuterMarginSize = 5;

    /// <summary>
    ///     The radius of the edges of the window
    /// </summary>
    private int _mWindowRadius = 10;

    /// <summary>
    ///     The last known dock position
    /// </summary>
    private WindowDockPosition _mDockPosition = WindowDockPosition.Undocked;

    /// <summary>
    ///     The rectangle border around the window when docked
    /// </summary>
    public int FlatBorderThickness => Borderless && _mWindow.WindowState != WindowState.Maximized ? 1 : 0;

    #endregion

    #region Public Properties

    /// <summary>
    ///     The current version of the app.
    /// </summary>
    public string Version { get; set; } =
        FileVersionInfo.GetVersionInfo(Assembly.GetCallingAssembly().Location).FileVersion;

    /// <summary>
    ///     The smallest width the window can go to.
    /// </summary>
    public double WindowMinimumWidth { get; set; }

    /// <summary>
    ///     The smallest height the window can go to.
    /// </summary>
    public double WindowMinimumHeight { get; set; }

    /// <summary>
    ///     True if the window should be borderless because it is docked or maximized
    /// </summary>
    public bool Borderless =>
        _mWindow.WindowState == WindowState.Maximized || _mDockPosition != WindowDockPosition.Undocked;

    /// <summary>
    ///     The size of the resize border around the window
    /// </summary>
    public int ResizeBorder => Borderless ? 0 : 1;

    /// <summary>
    ///     The size of the resize border around the window, taking into account the outer margin
    /// </summary>
    public Thickness ResizeBorderThickness => new(ResizeBorder + OuterMarginSize);

    /// <summary>
    ///     The padding of the inner content of the main window
    /// </summary>
    public Thickness InnerContentPadding { get; set; } = new(0);

    /// <summary>
    ///     The margin around the window to allow for a drop shadow
    /// </summary>
    public int OuterMarginSize
    {
        // If it is maximized or docked, no border
        get => Borderless ? 0 : _mOuterMarginSize;
        set => _mOuterMarginSize = value;
    }

    /// <summary>
    ///     The margin around the window to allow for a drop shadow
    /// </summary>
    public Thickness OuterMarginSizeThickness => new(OuterMarginSize);

    /// <summary>
    ///     The radius of the edges of the window
    /// </summary>
    public int WindowRadius
    {
        // If it is maximized or docked, no border
        get => Borderless ? 0 : _mWindowRadius;
        set => _mWindowRadius = value;
    }

    /// <summary>
    ///     The radius of the edges of the window
    /// </summary>
    public CornerRadius WindowCornerRadius => new(WindowRadius);

    #endregion

    #region Private Helpers

    /// <summary>
    ///     Gets the current mouse position on the screen
    /// </summary>
    /// <returns></returns>
    private Point GetMousePosition()
    {
        // Position of the mouse relative to the window
        var position = Mouse.GetPosition(_mWindow);

        // Add the window position so its a "ToScreen"
        if (_mWindow.WindowState == WindowState.Maximized)
            return new Point(position.X + _mWindowResizer.CurrentMonitorSize.Left,
                position.Y + _mWindowResizer.CurrentMonitorSize.Top);
        return new Point(position.X + _mWindow.Left, position.Y + _mWindow.Top);
    }

    /// <summary>
    ///     If the window resizes to a special position (docked or maximized)
    ///     this will update all required property change events to set the borders and radius values
    /// </summary>
    private void WindowResized()
    {
        // Fire off events for all properties that are affected by a resize
        OnPropertyChanged(nameof(Borderless));
        OnPropertyChanged(nameof(ResizeBorderThickness));
        OnPropertyChanged(nameof(OuterMarginSize));
        OnPropertyChanged(nameof(OuterMarginSizeThickness));
        OnPropertyChanged(nameof(WindowRadius));
        OnPropertyChanged(nameof(WindowCornerRadius));
    }

    #endregion
}