using System;
using System.Windows;
using AE.PID.ViewModels;
using ReactiveUI;

namespace AE.PID.Views;

public class ViewBase<TViewModel> : ReactiveUserControl<TViewModel> where TViewModel : ViewModelBase
{
    protected virtual void Close()
    {
        var window = Window.GetWindow(this);
        window?.Close();

        this.ViewModel = null;
    }
}