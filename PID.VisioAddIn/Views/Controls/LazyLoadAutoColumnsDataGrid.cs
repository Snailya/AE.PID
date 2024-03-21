using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using AE.PID.Tools;

namespace AE.PID.Views.Controls;

[DefaultEvent("LoadMore")]
public sealed class LazyLoadAutoColumnsDataGrid : AutoColumnsDataGrid
{
    private ScrollViewer? _scrollViewer;
    private const double Tolerance = 0.1;

    public LazyLoadAutoColumnsDataGrid()
    {
        IsReadOnly = true;

        VerticalScrollBarVisibility = ScrollBarVisibility.Visible;

        // get the scroller viewer form the visual tree so that could add custom LoadMore event to the scroller viewer 
        Loaded += LazyLoadDataGrid_Loaded;
        // unregister the above event when unloaded
        Unloaded += LazyLoadDataGrid_Unloaded;
    }

    public static readonly RoutedEvent LoadMoreEvent = EventManager.RegisterRoutedEvent("LoadMore",
        RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(ButtonBase));


    [Category("Behavior")]
    public event RoutedEventHandler LoadMore
    {
        add => AddHandler(LoadMoreEvent, value);
        remove => RemoveHandler(LoadMoreEvent, value);
    }

    private void OnLoadMore()
    {
        RaiseEvent(new RoutedEventArgs(LoadMoreEvent, this));
    }

    private void LazyLoadDataGrid_Unloaded(object sender, RoutedEventArgs e)
    {
        if (_scrollViewer != null)
            _scrollViewer.ScrollChanged -= _scrollViewer_ScrollChanged;
    }

    private void LazyLoadDataGrid_Loaded(object sender, RoutedEventArgs e)
    {
        _scrollViewer = this.FindVisualChild<ScrollViewer>();
        if (_scrollViewer != null)
            _scrollViewer.ScrollChanged += _scrollViewer_ScrollChanged;
    }

    private void _scrollViewer_ScrollChanged(object sender, ScrollChangedEventArgs e)
    {
        if (Math.Abs(e.VerticalOffset + e.ViewportHeight - e.ExtentHeight) < Tolerance)
            OnLoadMore();
    }
}