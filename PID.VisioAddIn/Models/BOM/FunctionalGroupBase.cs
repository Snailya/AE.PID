using System.Diagnostics.Contracts;
using Microsoft.Office.Interop.Visio;
using ReactiveUI;

namespace AE.PID.Models.BOM;

public abstract class FunctionalGroupBase : Element
{
    protected FunctionalGroupBase(Shape shape) : base(shape)
    {
        Contract.Assert(shape.HasCategory("FunctionalGroup"),
            "Only shape with category FunctionalGroup can be construct as FunctionalGroup");
    }

    protected override void OnInitialized()
    {
        Type = ElementType.FunctionalGroup;
        ParentId = 0;
        Designation = Source.CellsU["Prop.FunctionalGroup"].ResultStr[VisUnitCodes.visUnitsString];
        Description = Source.CellsU["Prop.FunctionalGroupDescription"].ResultStr[VisUnitCodes.visUnitsString];
    }

    protected override void OnCellChanged(Cell cell)
    {
        switch (cell.Name)
        {
            // bind No to Prop.FunctionalGroup
            case "Prop.FunctionalGroup":
                Designation = cell.ResultStr[VisUnitCodes.visUnitsString];
                this.RaisePropertyChanged(nameof(Label));
                break;
            // bind Description to Prop.FunctionalGroup
            case "Prop.FunctionalGroupDescription":
                Description = cell.ResultStr[VisUnitCodes.visUnitsString];
                break;
        }
    }
}