using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using AE.PID.Core.Models;
using AE.PID.Visio.Core.Interfaces;
using AE.PID.Visio.Core.Models;
using AE.PID.Visio.UI.Avalonia.Services;
using ReactiveUI;

namespace AE.PID.Visio.UI.Avalonia.ViewModels;

public class RecommendMaterialViewModel : ViewModelBase
{
    private readonly ObservableAsPropertyHelper<IEnumerable<MaterialRecommendationViewModel>>
        _data;

    private bool _isBusy;
    private MaterialRecommendationViewModel? _selected;

    public bool IsBusy
    {
        get => _isBusy;
        set => this.RaiseAndSetIfChanged(ref _isBusy, value);
    }

    public ReactiveCommand<Unit, MaterialViewModel> Confirm { get; private set; }
    public ReactiveCommand<Unit, Unit> Cancel { get; }


    /// <summary>
    ///     The suggested materials.
    /// </summary>
    public IEnumerable<MaterialRecommendationViewModel> Data => _data.Value;

    /// <summary>
    ///     The material in the recommendation collection that the user has selected
    /// </summary>
    public MaterialRecommendationViewModel? Selected
    {
        get => _selected;
        set => this.RaiseAndSetIfChanged(ref _selected, value);
    }

    #region -- Constructors --

    public RecommendMaterialViewModel(NotificationHelper notificationHelper, IMaterialService materialService,
        MaterialLocationContext context)
    {
        #region Commands

        var canConfirm = this.WhenAnyValue(x => x.Selected)
            .Select(x => x?.Value.Source.Id != null);
        Confirm = ReactiveCommand.Create(() =>
        {
            var material = Selected!.Value;
            materialService.FeedbackAsync(context, material.Source.Id, Selected!.CollectionId,
                Selected!.Id);
            return Selected!.Value;
        }, canConfirm);
        Cancel = ReactiveCommand.Create(() => { });

        #endregion
        
        // load the recommendations
        Observable.StartAsync(() => materialService.GetRecommendationAsync(context))
            .Catch<IEnumerable<Recommendation<Material>>, Exception>(e =>
            {
                notificationHelper.Error("加载失败", e.Message, NotificationHelper.Routes.SelectMaterial);

                return Observable.Return(Array.Empty<Recommendation<Material>>());
            })
            .Select(x => x.Select(i => new MaterialRecommendationViewModel(i)))
            .ToProperty(this, x => x.Data, out _data);
    }


    internal RecommendMaterialViewModel()
    {
        // design
    }

    #endregion
}