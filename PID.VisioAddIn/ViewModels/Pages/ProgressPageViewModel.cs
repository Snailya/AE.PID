using System;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using AE.PID.Visio.Core.Dtos;
using ReactiveUI;

namespace AE.PID.ViewModels;

public class ProgressPageViewModel(Progress<ProgressValueDto> progress, Action task) : ViewModelBase
{
    private bool _isExpanded;

    private ProgressValueDto _progressValueDto = new()
        { Value = 0, Status = TaskStatus.Created, Message = string.Empty };

    public ProgressValueDto ProgressValueDto
    {
        get => _progressValueDto;
        private set => this.RaiseAndSetIfChanged(ref _progressValueDto, value);
    }

    public bool IsExpanded
    {
        get => _isExpanded;
        set => this.RaiseAndSetIfChanged(ref _isExpanded, value);
    }

    public ReactiveCommand<Unit, Unit>? ToggleExpand { get; private set; }

    protected override void SetupCommands()
    {
        ToggleExpand = ReactiveCommand.Create(() => { IsExpanded = !IsExpanded; });
    }

    protected override void SetupSubscriptions(CompositeDisposable d)
    {
        Observable.FromEventPattern<ProgressValueDto>(
                handler => progress.ProgressChanged += handler,
                handler => progress.ProgressChanged -= handler
            )
            .Select(eventPattern => eventPattern.EventArgs)
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(progressValue => { ProgressValueDto = progressValue; }).DisposeWith(d);

        Observable.Start(task)
            .Subscribe(_ => { })
            .DisposeWith(d);
    }
}