using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using AE.PID.Tools;
using AE.PID.ViewModels;

namespace AE.PID.Views.Windows;

/// <summary>
///     Interaction logic for MainWindow.xaml
/// </summary>
public partial class WindowBase : IDisposable
{
    public enum WindowButton
    {
        CloseOnly,
        Normal,
        None
    }

    public static readonly DependencyProperty WindowButtonStyleProperty = DependencyProperty.Register(
        nameof(WindowButtonStyle), typeof(WindowButton), typeof(WindowBase),
        new PropertyMetadata(WindowButton.Normal));

    private readonly CompositeDisposable _cleanup = new();

    private bool _hasShow;

    private Dictionary<string, SizeAndLocation> SizeAndLocations { get; } = new();

    public WindowButton WindowButtonStyle
    {
        get => (WindowButton)GetValue(WindowButtonStyleProperty);
        set => SetValue(WindowButtonStyleProperty, value);
    }

    public void Dispose()
    {
        _cleanup.Dispose();
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        _hasShow = false;
        SaveSizeAndLocation();

        // close all child windows
        foreach (Window ownedWindow in OwnedWindows) ownedWindow.Close();

        Hide();
        Content = null;

        e.Cancel = true;
    }

    private void CenterOwner()
    {
        if (Owner != null) return;

        // GetWindowRect will return the rect in system resolution without scaling.
        // It means if I make a window full screen, it will return 1920 width even it is in 150 % DPI scaling for a screen resolution 1920x1080.
        Win32Ext.GetWindowRect(new IntPtr(Globals.ThisAddIn.Application.WindowHandle32), out var rect);

        var parentCenterX = (rect.Right + rect.Left) / 2;
        var parentCenterY = (rect.Bottom + rect.Top) / 2;

        // note that this not consider the dpi scaling,
        // for example, the actual width is 320 px,
        // while a snip tool measure result is 320 * 1.5 when scaling is 150 %
        var width = (Content as UserControl)!
            .Width; // 这还不是我想要的，因为ActualWidth可能会比Width大，但是在ContentRendered之前Actual又是0。为了解决这个问题，除非找到一个更好的位置调用CenterOwner，这个位置必须在Content渲染之后，但是又在窗口呈现直线，但是我还没发现。
        var height = (Content as UserControl)!.Height;

        var dpiScale = VisualTreeHelper.GetDpi(this);

        // however, the position of the window in WPF uses dpi related position.
        // that is for a 150% dpi scaling monitor, the mid-screen is 1920 / 1.5 / 2 is 640
        Left = parentCenterX / dpiScale.DpiScaleX - width / 2;
        Top = parentCenterY / dpiScale.DpiScaleY - height / 2;
    }

    private void ButtonBase_OnClick(object sender, RoutedEventArgs e)
    {
        if (e.OriginalSource is not Button button) return;
        switch (button.Name)
        {
            case "PART_Minimize":
                WindowState = WindowState.Minimized;
                break;
            case "PART_Maximize":
                WindowState = WindowState.Maximized;
                break;
            case "PART_Close":
                Close();
                break;
        }
    }

    /// <summary>
    ///     Restore size and location of the page from saved dictionary.
    /// </summary>
    public void RestoreSizeAndLocation()
    {
        var pageTitle = GetCurrentPageTitle();

        if (SizeAndLocations.TryGetValue(pageTitle, out var sizeAndLocation))
        {
            SizeToContent = SizeToContent.Manual;

            Left = sizeAndLocation.Left;
            Top = sizeAndLocation.Top;
            Width = sizeAndLocation.Width;
            Height = sizeAndLocation.Height;
        }
        else
        {
            SizeToContent = SizeToContent.WidthAndHeight;
            CenterOwner();
        }
    }

    /// <summary>
    ///     Get the tittle of the current page.
    /// </summary>
    /// <returns></returns>
    private string GetCurrentPageTitle()
    {
        return Content == null ? string.Empty : (string)((dynamic)Content).Title;
    }

    /// <summary>
    ///     Save the size and location of the current page if it has been registered in the dictionary.
    /// </summary>
    private void SaveSizeAndLocation()
    {
        var pageTitle = GetCurrentPageTitle();
        if (SizeAndLocations.ContainsKey(pageTitle))
            SizeAndLocations[pageTitle] = new SizeAndLocation(this);
    }

    /// <summary>
    ///     Register the current page to local dictionary if it meets the conditions: window is modified by user after show up.
    /// </summary>
    private void SetupSizeAndLocation()
    {
        Observable.FromEventPattern<EventHandler, System.EventArgs>(
                handler => ContentRendered += handler,
                handler => ContentRendered -= handler
            )
            .Subscribe(_ =>
            {
                if (Content != null) _hasShow = true;
            })
            .DisposeWith(_cleanup);

        Observable.FromEventPattern<SizeChangedEventHandler, SizeChangedEventArgs>(
                handler => SizeChanged += handler,
                handler => SizeChanged -= handler
            )
            .Select(_ => Unit.Default)
            .Merge(Observable.FromEventPattern<EventHandler, System.EventArgs>(
                    handler => LocationChanged += handler,
                    handler => LocationChanged -= handler
                )
                .Select(_ => Unit.Default))
            .Subscribe(_ =>
            {
                if (!_hasShow || Content == null) return;

                RegisterPage(GetCurrentPageTitle());
            })
            .DisposeWith(_cleanup);

        return;

        void RegisterPage(string name)
        {
            if (!SizeAndLocations.ContainsKey(name))
                SizeAndLocations.Add(name, new SizeAndLocation(this));
        }
    }


    private class SizeAndLocation(WindowBase window)
    {
        public double Height { get; } = window.ActualHeight;
        public double Width { get; } = window.ActualWidth;

        public double Left { get; } = window.Left;
        public double Top { get; } = window.Top;
    }

    #region Constructors

    protected WindowBase(Window owner) : this()
    {
        // if the parent is a WPF window
        Loaded += (_, _) => { Owner = owner; };
    }


    public WindowBase()
    {
        Title = Properties.Resources.PROPERTY_product_name;
        MaxHeight = SystemParameters.WorkArea.Height;
        MaxWidth = SystemParameters.WorkArea.Width;

        DataContext = new WindowViewModel(this);

        InitializeComponent();

        SetupSizeAndLocation();
    }

    #endregion
}