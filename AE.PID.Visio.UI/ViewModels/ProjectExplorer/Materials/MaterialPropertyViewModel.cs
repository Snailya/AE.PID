using AE.PID.Visio.Core.Models;

namespace AE.PID.Visio.UI.Avalonia.ViewModels;

public class MaterialPropertyViewModel : ViewModelBase
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