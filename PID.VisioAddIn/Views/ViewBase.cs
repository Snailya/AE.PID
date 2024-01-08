using System.Windows;
using ReactiveUI;

namespace AE.PID.Views;

public class ViewBase<T> : ReactiveUserControl<T> where T : class
{
    protected virtual void Close()
    {
        var window = Window.GetWindow(this);
        window?.Close();
    }
}