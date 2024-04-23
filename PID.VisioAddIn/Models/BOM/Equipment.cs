using System.Diagnostics.Contracts;
using AE.PID.Tools;
using Microsoft.Office.Interop.Visio;
using ReactiveUI;

namespace AE.PID.Models.BOM;

public class Equipment : PartItem
{
    private string _subClassName = string.Empty;

    #region Consturctors

    public Equipment(Shape shape) : base(shape)
    {
        Contract.Assert(shape.HasCategory("Equipment") || shape.HasCategory("Instrument"),
            "Only shape with category Equipment or Instrument can be construct as Equipment");
    }

    #endregion

    #region Properties

    public string SubClassName
    {
        get => _subClassName;
        set => this.RaiseAndSetIfChanged(ref _subClassName, value);
    }

    #endregion

    #region Methods Overrides

    protected override void OnRelationshipsChanged(Cell cell)
    {
        base.OnRelationshipsChanged(cell);

        ParentId = GetContainerIdByCategory(Source, "Unit") ??
                   GetContainerIdByCategory(Source, "FunctionalGroup") ?? 0;
    }

    protected override void OnCellChanged(Cell cell)
    {
        base.OnCellChanged(cell);

        switch (cell.Name)
        {
            // bind Description to Prop.Description
            case "Prop.FunctionalElement":
                Designation = Source.TryGetFormatValue("Prop.FunctionalElement") ?? string.Empty;
                this.RaisePropertyChanged(nameof(Label));
                break;
            // bind Description to Prop.Description
            case "Prop.Description":
                Description = cell.ResultStr[VisUnitCodes.visUnitsString];
                break;
            // bind Description to Prop.FunctionalGroup
            case "Prop.SubClass":
                SubClassName = cell.ResultStr[VisUnitCodes.visUnitsString];
                break;
            // bind Description to Prop.FunctionalGroup
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

    public override string GetFunctionalElement()
    {
        return Designation;
    }

    public override string GetName()
    {
        return SubClassName;
    }

    protected override void OnInitialized()
    {
        base.OnInitialized();

        Type = ElementType.Equipment;
        ParentId = GetContainerIdByCategory(Source, "Unit") ??
                   GetContainerIdByCategory(Source, "FunctionalGroup") ?? 0;
        Designation = Source.TryGetFormatValue("Prop.FunctionalElement") ?? string.Empty;
        MaterialNo = Source.TryGetFormatValue("Prop.D_BOM") ?? string.Empty;
        SubClassName = Source.CellsU["Prop.SubClass"].ResultStr[VisUnitCodes.visUnitsString];
        if (double.TryParse(Source.Cells["Prop.Subtotal"].ResultStr[VisUnitCodes.visUnitsString],
                out var value))
            Count = value;
    }

    #endregion
}