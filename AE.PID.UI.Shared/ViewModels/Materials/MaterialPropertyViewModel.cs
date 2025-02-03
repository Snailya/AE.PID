using AE.PID.Client.Core;
using ReactiveUI;

namespace AE.PID.UI.Shared;

public class MaterialPropertyViewModel : ReactiveObject
{
    public MaterialPropertyViewModel(MaterialProperty source)
    {
        Name = source.Name;
        Value = source.Value;
    }

    public MaterialPropertyViewModel()
    {
        // design
    }

    public string Name { get; set; }
    public string Value { get; set; }
}