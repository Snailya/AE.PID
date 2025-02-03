using System.Reactive.Disposables;
using ReactiveUI;
using Splat;

namespace AE.PID.UI.Shared;

public abstract class ViewModelBase : ReactiveObject, IActivatableViewModel, IEnableLogger
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

    protected CompositeDisposable CleanUp { get; } = new();

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

    /// <summary>
    ///     This method invokes when the view model is collected by GC. By default, it is when new view model seted.
    /// </summary>
    protected virtual void SetupDeactivate()
    {
    }
}