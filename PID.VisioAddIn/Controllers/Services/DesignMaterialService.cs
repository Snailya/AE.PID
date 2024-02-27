using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
}