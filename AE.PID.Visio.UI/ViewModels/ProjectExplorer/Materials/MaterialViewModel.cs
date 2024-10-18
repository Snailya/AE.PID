using System.Collections.Generic;
using System.Linq;
using AE.PID.Visio.Core.Models;
using ReactiveUI;

namespace AE.PID.Visio.UI.Avalonia.ViewModels;

public class MaterialViewModel : ReactiveObject
{
    public MaterialViewModel(Material material)
    {
        Source = material;

        Name = material.Name;
        Brand = material.Brand;
        Specifications = material.Specifications;
        Type = material.Type;
        Unit = material.Unit;
        Supplier = material.Supplier;
        ManufacturerMaterialNumber = material.ManufacturerMaterialNumber;
        TechnicalDataEnglish = material.TechnicalDataEnglish;
        TechnicalData = material.TechnicalData;
        Properties = material.Properties.Any()
            ? material.Properties.Where(x => !string.IsNullOrEmpty(x.Value))
                .Select(x => new MaterialPropertyViewModel(x))
            : [];
    }

    internal MaterialViewModel()
    {
        // Design
    }


    public Material Source { get; set; }

    public string Code => Source.Code;

    public string Name { get; set; }

    public string Brand { get; set; }

    public string Specifications { get; set; }

    public string Type { get; set; }

    public string Unit { get; set; }

    public string Supplier { get; set; }

    public string ManufacturerMaterialNumber { get; set; }

    public string TechnicalDataEnglish { get; set; }

    public string TechnicalData { get; set; }

    public IEnumerable<MaterialPropertyViewModel> Properties { get; set; }
}