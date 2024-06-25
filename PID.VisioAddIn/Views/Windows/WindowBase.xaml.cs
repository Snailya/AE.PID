using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reactive.Disposables;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
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
    private string prevPage = string.Empty;

    private Dictionary<string, SizeAndLocation> Locations { get; } = new();

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
        StoreLocation();

        // close all child windows
        foreach (Window ownedWindow in OwnedWindows) ownedWindow.Close();

        Hide();
        Content = null;

        e.Cancel = true;
    }

    private void StoreLocation()
    {
        var title = GetCurrentPageTitle();

        // if a window not be resized or moved during the open duration,
        // and it also not been record in the previous open duration, skip this process.
        if (DataContext is not WindowViewModel { IsResizedOrMoved: true } && !Locations.ContainsKey(title)) return;

        // add or update the location
        if (!Locations.ContainsKey(title))
            Locations.Add(title, new SizeAndLocation(this));
        else
            Locations[title] = new SizeAndLocation(this);
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
        var width = DesiredSize.Width;
        var height = DesiredSize.Height;

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
    ///     Restore the size and location of the page from saved dictionary.
    /// </summary>
    private void RestoreLocation(SizeAndLocation location)
    {
        Left = location.Left;
        Top = location.Top;
        Width = location.Width;
        Height = location.Height;
    }


    /// <summary>
    ///     Get the tittle of the current page.
    /// </summary>
    /// <returns></returns>
    private string GetCurrentPageTitle()
    {
        return Content == null ? string.Empty : (string)((dynamic)Content).Title;
    }

    protected override void OnContentChanged(object oldContent, object? newContent)
    {
        // reset the size to content back to width and height
        if (newContent == null)
        {
            (DataContext as WindowViewModel)!.IsResizedOrMoved = false;
            SizeToContent = SizeToContent.WidthAndHeight;
            //prevPage = (string)((dynamic)oldContent).Title;
        }

        base.OnContentChanged(oldContent, newContent);
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


    protected WindowBase()
    {
        Title = Properties.Resources.PROPERTY_product_name;
        MaxHeight = SystemParameters.WorkArea.Height;
        MaxWidth = SystemParameters.WorkArea.Width;

        DataContext = new WindowViewModel(this);

        InitializeComponent();

        LayoutUpdated += (sender, args) =>
        {
            if (Content == null) return;

            // fix the window size on first show
            SizeToContent = SizeToContent.Manual;

            var currPage = GetCurrentPageTitle();
            if (currPage == prevPage) return;

            // if there is a saved location for the page, restore the location
            if (Locations.TryGetValue(currPage, out var location))
            {
                RestoreLocation(location);
            }
            else
            {
                // if not, there are 2 situations
                // 1. the window is the root window, then it should be centered if it differs from the previous page
                // 2. the window is the child window, it should not be applied Center owner
                if (Owner == null)
                    CenterOwner();
            }

            prevPage = currPage;
        };
    }

    public WindowBase(IntPtr parentHandle) : this()
    {
        var _ = new WindowInteropHelper(this)
        {
            Owner = parentHandle
        };
    }

    #endregion
}