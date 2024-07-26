using System.Diagnostics.Contracts;
using System.Linq;
using AE.PID.Tools;
using Microsoft.Office.Interop.Visio;

namespace AE.PID.Models;

public sealed class FunctionalElement : PartItem
{
    #region Constructors

    public FunctionalElement(Shape shape) : base(shape)
    {
        Contract.Assert(shape.HasCategory("FunctionalElement"),
            "Only shape with category FunctionalElement can be construct as FunctionalElement");
    }

    #endregion

    #region Methods Overrides

    protected override void OnRelationshipsChanged(Cell cell)
    {
        base.OnRelationshipsChanged(cell);

        ParentId = GetAssociatedEquipment(Source) ?? 0;
    }

    private static int? GetAssociatedEquipment(IVShape shape)
    {
        var target = shape.CalloutTarget;
        if (target == null) return null;
        if (target.HasCategory("Equipment")) return target.ID;
        return null;
    }

    public override string GetFunctionalElement()
    {
        var parent = Source.ContainingPage.Shapes.OfType<Shape>().SingleOrDefault(x => x.ID == ParentId);
        if (parent == null) return Designation;

        var parentDesignation = parent.TryGetFormatValue("Prop.FunctionalElement");
        return string.IsNullOrEmpty(parentDesignation) ? Designation : $"{parentDesignation}-{Designation}";
    }


    protected override void OnInitialized()
    {
        base.OnInitialized();

        Type = ElementType.FunctionalElement;
        ParentId = GetAssociatedEquipment(Source) ?? 0;
    }

    #endregion
}