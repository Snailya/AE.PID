using AE.PID.ViewModels;

namespace AE.PID.EventArgs;

public class MaterialLocationSelectedEventArgs(MaterialLocationViewModel materialLocation)
{
    public MaterialLocationViewModel MaterialLocation { get; } = materialLocation;
}