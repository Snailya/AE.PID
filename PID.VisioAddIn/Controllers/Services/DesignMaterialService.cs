using System;
using System.Collections.Generic;
using System.Linq;
using AE.PID.ViewModels;
using DynamicData;

namespace AE.PID.Controllers.Services;

public class DesignMaterialService
{
    private readonly SourceList<DesignMaterialViewModel> _materials = new();

    public DesignMaterialService()
    {
    }

    public IObservableList<DesignMaterialViewModel> Materials => _materials.AsObservableList();
    
    
    public IEnumerable<string> ReloadMaterials(string name)
    {
        _materials.Clear();

        // todo: get from server
        var random = new Random();
        var count = random.Next(1, 3);
        for (var i = 0; i < count; i++)
        {
            var item = new DesignMaterialViewModel(i.ToString(), $"{name}{i}");
            for (var j = 0; j < count; j++)
            {
                var property = new MaterialProperty($"P{j}", $"V{j}");
                item.Properties.Add(property);
            }
            _materials.Add(item);
        }

        var columns = _materials.Items.FirstOrDefault().Properties.Select(x => x.Name);

        return columns;
    }
}