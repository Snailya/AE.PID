﻿using System;
using System.Collections.ObjectModel;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using AE.PID.EventArgs;
using AE.PID.Models;
using AE.PID.Services;
using DynamicData;
using DynamicData.Binding;
using ReactiveUI;
using Splat;
using SaveFileDialog = Microsoft.Win32.SaveFileDialog;
using TaskStatus = AE.PID.Core.Models.TaskStatus;

namespace AE.PID.ViewModels;

public class ProjectExplorerPageViewModel(ProjectService? service = null) : ViewModelBase
{
    private readonly ProjectService _service = service ?? Locator.Current.GetService<ProjectService>()!;
    private PartItem? _copySource;
    private ReadOnlyObservableCollection<TreeNodeViewModel<ElementBase>> _elementTree = new([]);
    private ObservableAsPropertyHelper<bool> _isElementsLoading = ObservableAsPropertyHelper<bool>.Default();
    private ReadOnlyObservableCollection<PartItem> _partListItems = new([]);
    private ElementBase? _selected;

    #region Output Properties

    public ReadOnlyObservableCollection<TreeNodeViewModel<ElementBase>> ElementTree => _elementTree;
    public ReadOnlyObservableCollection<PartItem> PartListItems => _partListItems;

    public bool IsElementsLoading => _isElementsLoading.Value;

    #endregion

    #region Read-Only Properties

    public DocumentInfoViewModel DocumentInfo { get; } = new();
    public OkCancelFeedbackViewModel OkCancelFeedbackViewModel { get; } = new();
    public ReactiveCommand<Unit, Unit>? CopyMaterial { get; private set; }
    public ReactiveCommand<Unit, Unit>? PasteMaterial { get; private set; }
    public ReactiveCommand<Unit, Unit>? ExportToPage { get; private set; }

    #endregion

    #region Setups

    protected override void SetupCommands()
    {
        OkCancelFeedbackViewModel.Ok = ReactiveCommand.Create(() =>
        {
            var dialog = new SaveFileDialog
            {
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                Filter = @"Excel Files|*.xlsx|All Files|*.*""",
                Title = @"保存文件"
            };
            if (dialog.ShowDialog() is true) _service.ExportToExcel(dialog.FileName, DocumentInfo);
        });
        OkCancelFeedbackViewModel.Cancel = ReactiveCommand.Create(() => { });

        ExportToPage = ReactiveCommand.Create(_service.ExportToPage);

        // copy design material is allowed if the selected item has material no
        var canCopy = this.WhenAnyValue(x => x.Selected,
            x => x is PartItem partItem && !string.IsNullOrEmpty(partItem.MaterialNo));
        CopyMaterial = ReactiveCommand.Create(() => { CopySource = (PartItem)Selected!; }, canCopy);

        // paste material is allowed if the item is of the same type, that is only copy equipment to equipment, instrument to instrument
        var canPaste = this.WhenAnyValue(x => x.CopySource, x => x.Selected,
            (source, target) => source != null && target != null && source.GetType() == target.GetType());
        PasteMaterial = ReactiveCommand.Create(() =>
        {
            if (Selected is PartItem partItem && CopySource != null)
                partItem.CopyMaterialFrom(CopySource);
        }, canPaste);
    }

    protected override void SetupSubscriptions(CompositeDisposable d)
    {
        Observable.Create<TaskStatus>(observer =>
            {
                observer.OnNext(TaskStatus.Created);

                try
                {
                    observer.OnNext(TaskStatus.Running);
                    _service.LoadElements();
                }
                catch (Exception e)
                {
                    observer.OnError(e);
                }

                observer.OnNext(TaskStatus.RanToCompletion);
                observer.OnCompleted();

                return () => { };
            })
            .SubscribeOn(AppScheduler.VisioScheduler)
            .ObserveOn(AppScheduler.UIScheduler)
            .Select(x => x == TaskStatus.Running)
            .ToProperty(this, x => x.IsElementsLoading, out _isElementsLoading)
            .DisposeWith(d);

        _service.Elements
            .Connect()
            .AutoRefresh(t => t.ParentId)
            .TransformToTree(x => x.ParentId)
            .Transform(x => new TreeNodeViewModel<ElementBase>(x))
            .Sort(SortExpressionComparer<TreeNodeViewModel<ElementBase>>.Ascending(t => t.Source!.Label))
            .ObserveOn(AppScheduler.UIScheduler)
            .Bind(out _elementTree)
            .DisposeMany()
            .Subscribe()
            .DisposeWith(d);

        _service.Elements
            .Connect()
            .Filter(x => x is PartItem)
            .Transform(x => (PartItem)x)
            .ObserveOn(AppScheduler.UIScheduler)
            .Bind(out _partListItems)
            .DisposeMany()
            .Subscribe()
            .DisposeWith(d);

        // whenever there is a selected element, notify the View to show the side page
        var selectedItem = this.WhenAnyValue(x => x.Selected)
            .WhereNotNull()
            .DistinctUntilChanged()
            .ObserveOn(AppScheduler.UIScheduler);
        selectedItem.Subscribe()
            .DisposeWith(d);

        // notify the side view model for seeding
        MessageBus.Current.RegisterMessageSource(selectedItem.Select(x =>
                new ElementSelectedEventArgs(x)))
            .DisposeWith(d);
    }

    protected override void SetupStart()
    {
        AppScheduler.VisioScheduler.Schedule(() => _service.Start());
    }

    protected override void SetupDeactivate()
    {
        AppScheduler.VisioScheduler.Schedule(() => _service.Stop());
    }

    #endregion

    #region Read-Write Properties

    public ElementBase? Selected
    {
        get => _selected;
        set => this.RaiseAndSetIfChanged(ref _selected, value);
    }

    public PartItem? CopySource
    {
        get => _copySource;
        set => this.RaiseAndSetIfChanged(ref _copySource, value);
    }

    #endregion
}