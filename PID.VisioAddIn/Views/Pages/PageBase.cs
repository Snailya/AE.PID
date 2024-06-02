using System.Windows;
using AE.PID.ViewModels;
using AE.PID.Views.Windows;

namespace AE.PID.Views;

public class PageBase<TViewModel> : ViewBase<TViewModel> where TViewModel : ViewModelBase
{
    protected PageBase()
    {
        Padding = new Thickness(8);

        Unloaded += (_, _) => { ViewModel = null; };
    }
}