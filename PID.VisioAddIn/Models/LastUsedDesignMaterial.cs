using System;

namespace AE.PID.Models;

public class LastUsedDesignMaterial(DesignMaterial source)
{
    /// <summary>
    ///     The last time that user selects the design material.
    ///     Used for sorting favorites.
    /// </summary>
    public DateTime LastUsed { get; set; } = DateTime.Now;

    public DesignMaterial Source { get; set; } = source;
}