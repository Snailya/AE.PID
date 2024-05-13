using System.Windows;
using System.Windows.Controls;

namespace AE.PID.Views;

public class TreeListView : TreeView
{
    public static readonly DependencyProperty ViewProperty =
        DependencyProperty.Register(nameof(View), typeof(ViewBase), typeof(TreeListView));
    
    public ViewBase View
    {
        get => (ViewBase)GetValue(ViewProperty);
        set => SetValue(ViewProperty, value);
    }
    
    protected override DependencyObject GetContainerForItemOverride() //创建或标识用于显示指定项的元素。 
    {
        return new TreeListViewItem();
    }

    protected override bool IsItemItsOwnContainerOverride(object item) //确定指定项是否是（或可作为）其自己的 ItemContainer
    {
        var isTreeLvi = item is TreeListViewItem;
        return isTreeLvi;
    }
}