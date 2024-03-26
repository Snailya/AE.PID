using System.Reactive.Disposables;
using ReactiveUI;

namespace AE.PID.ViewModels;

public abstract class ViewModelBase : ReactiveObject, IActivatableViewModel
{
    protected ViewModelBase()
    {
        Activator = new ViewModelActivator();

        this.WhenActivated(disposableRegistration =>
        {
            Disposable.Create(SetupDeactivate).DisposeWith(disposableRegistration);

            SetupCommands();
            SetupSubscriptions(disposableRegistration);
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