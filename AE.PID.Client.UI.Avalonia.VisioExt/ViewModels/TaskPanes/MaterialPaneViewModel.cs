using System.Reactive.Linq;
using AE.PID.Client.Core;
using AE.PID.Client.UI.Avalonia.Shared;
using ReactiveUI;

namespace AE.PID.Client.UI.Avalonia.VisioExt;

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

                if (await materialResolver.ResolvedAsync(x) is { } resolved)
                    return new MaterialViewModel(resolved.Value);

                return null;
            })
            .ToProperty(this, x => x.Material, out _material);
    }

    internal MaterialPaneViewModel()
    {
    }

    public MaterialViewModel? Material => _material.Value;

    public string Code
    {
        get => _code;
        set => this.RaiseAndSetIfChanged(ref _code, value);
    }
}