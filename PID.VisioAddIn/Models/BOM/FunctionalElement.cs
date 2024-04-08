using System.Diagnostics.Contracts;
using System.Linq;
using AE.PID.Models.VisProps;
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

        Initialize();
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

    protected override void OnRelationshipsChanged()
    {
        base.OnRelationshipsChanged();

        ParentId = GetAssociate(Source) ?? 0;
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

    protected override void Initialize()
    {
        base.Initialize();

        Type = ElementType.FunctionalElement;
        ParentId = GetAssociate(Source) ?? 0;
        Designation = Source.CellsU["Prop.FunctionalElement"].ResultStr[VisUnitCodes.visUnitsString];
        Description = Source.CellsU["Prop.Description"].ResultStr[VisUnitCodes.visUnitsString];
    }

    #endregion
}