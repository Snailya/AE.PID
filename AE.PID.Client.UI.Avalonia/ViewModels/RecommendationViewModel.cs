using System;
using AE.PID.Client.Core;
using AE.PID.Client.UI.Avalonia.Shared;
using ReactiveUI;

namespace AE.PID.Client.UI.Avalonia;

public class RecommendationViewModel<TModel, TViewModel>(Recommendation<TModel> recommendation)
    where TViewModel : ReactiveObject
{
    public int Id { get; set; } = recommendation.Id;
    public int CollectionId { get; set; } = recommendation.Id;
    public string Algorithm { get; set; } = recommendation.Algorithm;
    public TViewModel Value { get; } = (TViewModel)Activator.CreateInstance(typeof(TViewModel), recommendation.Data);
}

public class MaterialRecommendationViewModel(Recommendation<Material> recommendation)
    : RecommendationViewModel<Material, MaterialViewModel>(recommendation)
{
}