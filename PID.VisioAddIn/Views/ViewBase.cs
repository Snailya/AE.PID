using System;
using System.Windows;
using ReactiveUI;

namespace AE.PID.Views;

public class ViewBase<TViewModel> : ReactiveUserControl<TViewModel> where TViewModel : class
{
    protected virtual void Close()
    {
        var window = Window.GetWindow(this);
        window?.Close();
    }
}