using System;
using System.Threading.Tasks;
using AE.PID.Client.Core;
using ReactiveUI;

namespace AE.PID.Client.UI.Avalonia;

public class InstrumentViewModel:MaterialLocationViewModel
{
    public InstrumentViewModel(MaterialLocation material, FunctionLocation function, Lazy<Task<ResolveResult<Material?>>> materialLoader) : base(material, function, materialLoader)
    {
    }

    public InstrumentViewModel(MaterialLocation location, Lazy<Task<ResolveResult<Material?>>> material) : base(location, material)
    {
    }
}