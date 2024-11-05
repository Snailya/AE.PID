using System;
using AE.PID.Visio.Core.Models;
using ReactiveUI;

namespace AE.PID.Visio.UI.Avalonia.ViewModels;

public class RecommendationViewModel<TModel, TViewModel>(Recommendation<TModel> recommendation)
    where TViewModel : ReactiveObject
{
    public int Id { get; set; } = recommendation.Id;
    public int CollectionId { get; set; } = recommendation.Id;
    public string Algorithm { get; set; } = recommendation.Algorithm;
    public TViewModel Value { get; } = (TViewModel)Activator.CreateInstance(typeof(TViewModel), recommendation.Data);
}

public class MaterialRecommendationViewModel(Recommendation<Material> recommendation) : RecommendationViewModel<Material, MaterialViewModel>(recommendation)
{}