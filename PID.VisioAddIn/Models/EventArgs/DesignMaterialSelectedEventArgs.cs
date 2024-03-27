using AE.PID.Models.BOM;

namespace AE.PID.Models.EventArgs;

public class DesignMaterialSelectedEventArgs(DesignMaterial designMaterial)
{
    public DesignMaterial DesignMaterial { get; } = designMaterial;
}