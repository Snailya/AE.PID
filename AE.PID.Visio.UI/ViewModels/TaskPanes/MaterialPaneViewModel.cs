﻿using System.Reactive.Linq;
using AE.PID.Visio.Core.Interfaces;
using ReactiveUI;

namespace AE.PID.Visio.UI.Avalonia.ViewModels;

public class MaterialPaneViewModel : ViewModelBase
{
    private readonly ObservableAsPropertyHelper<MaterialViewModel?> _material;
    private string _code = string.Empty;

    public MaterialPaneViewModel(IMaterialResolver materialResolver)
    {
        this.WhenAnyValue(x => x.Code)
            .SelectMany(async x =>
            {
                if (string.IsNullOrEmpty(x)) return null;
                
                if (await materialResolver.GetMaterialByCodeAsync(x) is { } resolved)
                    return new MaterialViewModel(resolved.Value);
                
                return null;
            })
            .ToProperty(this, x => x.Material, out _material);
    }

    public MaterialViewModel? Material => _material.Value;

    public string Code
    {
        get => _code;
        set => this.RaiseAndSetIfChanged(ref _code, value);
    }
}