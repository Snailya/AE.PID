using System.Windows;
using System.Windows.Controls;

namespace AE.PID.Views.Controls;

public class TreeListView : TreeView
{
    protected override DependencyObject GetContainerForItemOverride() //创建或标识用于显示指定项的元素。 
    {
        return new TreeListViewItem();
    }

    protected override bool IsItemItsOwnContainerOverride(object item) //确定指定项是否是（或可作为）其自己的 ItemContainer
    {
        //return item is TreeListViewItem;
        var isTreeLvi = item is TreeListViewItem;
        return isTreeLvi;
    }
}