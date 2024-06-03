using System;
using System.Reactive.Linq;
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
            .Subscribe(progressValue =>
            {
                ProgressValue = progressValue;
            });

        Observable.Start(task)
            .SubscribeOn(ThisAddIn.Dispatcher!)
            .Subscribe(_ => { });
    }


    public ProgressValue ProgressValue
    {
        get => _progressValue;
        private set => this.RaiseAndSetIfChanged(ref _progressValue, value);
    }
}