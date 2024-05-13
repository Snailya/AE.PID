using System.Windows;
using AE.PID.ViewModels;

namespace AE.PID.Views;

public class PageBase<TViewModel> : ViewBase<TViewModel> where TViewModel : ViewModelBase
{
    protected PageBase()
    {
        Padding = new Thickness(8);
    }
}