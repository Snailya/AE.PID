using System.Diagnostics.Contracts;
using Microsoft.Office.Interop.Visio;

namespace AE.PID.Models;

public class ProxyFunctionalGroup : FunctionalGroupBase
{
    public ProxyFunctionalGroup(Shape shape) : base(shape)
    {
        Contract.Assert(shape.HasCategory("Proxy"),
            "Only shape with category Proxy can be construct as ProxyFunctionalGroup");
    }
}