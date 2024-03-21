using System;
using System.Collections.Generic;

namespace AE.PID.Models.BOM;

public class LastUsedDesignMaterial(DesignMaterial source)
{
    /// <summary>
    /// The last time that the design material is selected by user.
    /// Used for sorting favorites.
    /// </summary>
    public DateTime LastUsed { get; set; } = DateTime.Now;

    public HashSet<string> UsedBy { get; set; } = [];
    public DesignMaterial Source { get; set; } = source;
}