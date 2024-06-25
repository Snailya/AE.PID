using System.Windows;
using AE.PID.ViewModels;

namespace AE.PID.Views;

public class PageBase<TViewModel> : ViewBase<TViewModel> where TViewModel : ViewModelBase
{
    private Window? _window;

    protected PageBase(string title)
    {
        Title = title;
        Padding = new Thickness(8);
    }

    public string Title { get; }
    public double ComputedHeight { get; set; }

    public Size? ComputedSize { get; set; }

    protected override Size MeasureOverride(Size constraint)
    {
        var size = base.MeasureOverride(constraint);
        ComputedSize ??= base.MeasureOverride(constraint);
        return size;
    }

    protected override Size ArrangeOverride(Size arrangeBounds)
    {
        var size = base.ArrangeOverride(arrangeBounds);
        return size;
    }
}