using System.Windows;
using AE.PID.ViewModels;

namespace AE.PID.Views;

public class PageBase<TViewModel> : ViewBase<TViewModel> where TViewModel : ViewModelBase
{
    protected PageBase(string title)
    {
        Title = title;
        Padding = new Thickness(8);
    }

    public string Title { get; }
}