using System.Windows;
using System.Windows.Controls;

namespace AE.PID.Views;

public class TreeListViewItem : TreeViewItem
{
    private int _level = -1;

    public int Level
    {
        get
        {
            if (_level != -1) return _level;

            _level = ItemsControlFromItemContainer(this) is TreeListViewItem parent ? parent.Level + 1 : 0;
            return _level;
        }
    }

    protected override DependencyObject GetContainerForItemOverride()
    {
        return new TreeListViewItem();
    }

    protected override bool IsItemItsOwnContainerOverride(object item)
    {
        var isItv = item is TreeListViewItem;
        return isItv;
    }
}