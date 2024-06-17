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

        Loaded += (_, _) =>
        {
            _window = Window.GetWindow(this);
            _window!.SizeToContent = SizeToContent.Manual;
        };
        Unloaded += (_, _) =>
        {
            ViewModel = null;
            _window!.SizeToContent = SizeToContent.WidthAndHeight;
        };
    }

    public string Title { get; }
}