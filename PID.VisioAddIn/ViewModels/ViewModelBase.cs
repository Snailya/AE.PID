using System.Reactive.Disposables;
using ReactiveUI;

namespace AE.PID.ViewModels;

public abstract class ViewModelBase : ReactiveObject, IActivatableViewModel
{
    protected ViewModelBase()
    {
        Activator = new ViewModelActivator();

        this.WhenActivated(d =>
        {
            Disposable.Create(SetupDeactivate).DisposeWith(d);

            SetupCommands();
            SetupSubscriptions(d);
            SetupStart();
        });
    }

    public ViewModelActivator Activator { get; }

    protected virtual void SetupCommands()
    {
    }

    protected virtual void SetupSubscriptions(CompositeDisposable d)
    {
    }

    protected virtual void SetupStart()
    {
    }

    protected virtual void SetupDeactivate()
    {
    }
}