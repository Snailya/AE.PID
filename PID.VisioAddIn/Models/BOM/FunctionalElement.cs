using System.Diagnostics.Contracts;
using System.Linq;
using AE.PID.Tools;
using Microsoft.Office.Interop.Visio;
using ReactiveUI;

namespace AE.PID.Models.BOM;

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

    protected override void OnCellChanged(Cell cell)
    {
        base.OnCellChanged(cell);

        switch (cell.Name)
        {
            // bind FunctionalGroup to Prop.FunctionalGroup
            case "Prop.FunctionalGroup":
                FunctionalGroup = Source.TryGetFormatValue("Prop.FunctionalGroup") ?? string.Empty;
                break;
            // bind Description to Prop.Description
            case "Prop.FunctionalElement":
                Designation = cell.ResultStr[VisUnitCodes.visUnitsString];
                this.RaisePropertyChanged(nameof(Label));
                break;
            // bind Description to Prop.Description
            case "Prop.Description":
                Description = cell.ResultStr[VisUnitCodes.visUnitsString];
                break;
            case "Prop.Subtotal":
                if (double.TryParse(Source.Cells["Prop.Subtotal"].ResultStr[VisUnitCodes.visUnitsString],
                        out var subtotal))
                    Count = subtotal;
                break;
            case "Prop.D_BOM":
                MaterialNo = Source.TryGetFormatValue("Prop.D_BOM") ?? string.Empty;
                break;
        }
    }

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

    public override string GetName()
    {
        return string.Empty;
    }

    protected override void OnInitialized()
    {
        base.OnInitialized();

        Type = ElementType.FunctionalElement;
        ParentId = GetAssociatedEquipment(Source) ?? 0;
        Designation = Source.CellsU["Prop.FunctionalElement"].ResultStr[VisUnitCodes.visUnitsString];
        Description = Source.CellsU["Prop.Description"].ResultStr[VisUnitCodes.visUnitsString];
    }

    #endregion
}