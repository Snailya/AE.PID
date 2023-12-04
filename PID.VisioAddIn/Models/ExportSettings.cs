using System;
using System.Collections.Generic;

namespace AE.PID.Models;

[Serializable]
public class ExportSettings
{
    public IList<string> BOMLayers { get; set; } = new List<string> { "Equipments" };
}