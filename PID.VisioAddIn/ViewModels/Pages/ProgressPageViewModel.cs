using System;
using System.Diagnostics;
using System.Reactive.Linq;
using System.Threading;
using AE.PID.Services;
using ReactiveUI;

namespace AE.PID.ViewModels;

public class ProgressPageViewModel : ViewModelBase
{
    private ProgressValue _progressValue = new() { Value = 0, Status = TaskStatus.Created, Message = string.Empty };

    public ProgressPageViewModel(Progress<ProgressValue> progress, Action task)
    {
        Observable.FromEventPattern<ProgressValue>(
                handler => progress.ProgressChanged += handler,
                handler => progress.ProgressChanged -= handler
            )
            .Select(eventPattern => eventPattern.EventArgs)
            .ObserveOn(RxApp.MainThreadScheduler)
            .Do(x => Debug.WriteLine($"Observe progress change on {Thread.CurrentThread.Name}"))
            .Subscribe(progressValue => { ProgressValue = progressValue; });

        Observable.Start(task)
            // .SubscribeOn(ThisAddIn.Dispatcher!)
            .Do(x => Debug.WriteLine($"Observe task on {Thread.CurrentThread.Name}"))
            .Subscribe(_ => { });
    }


    public ProgressValue ProgressValue
    {
        get => _progressValue;
        private set => this.RaiseAndSetIfChanged(ref _progressValue, value);
    }
}