using System.Diagnostics.Contracts;
using System.Linq;
using Microsoft.Office.Interop.Visio;
using ReactiveUI;

namespace AE.PID.Models.BOM;

public sealed class EquipmentUnit : Element
{
    private double _count;

    #region Constructors

    public EquipmentUnit(Shape shape) : base(shape)
    {
        Contract.Assert(shape.HasCategory("Unit"),
            "Only shape with category Unit can be construct as EquipmentUnit");
    }

    #endregion

    #region Properties

    public double Count
    {
        get => _count;
        private set => this.RaiseAndSetIfChanged(ref _count, value);
    }

    #endregion

    #region Methods Overrides

    protected override void OnRelationshipsChanged(Cell cell)
    {
        base.OnRelationshipsChanged(cell);

        // update parent id
        var containerIds = Source.MemberOfContainers.OfType<int>().ToArray();
        if (!containerIds.Any())
        {
            ParentId = 0;
        }
        else
        {
            var containers = containerIds.Select(x => Source.ContainingPage.Shapes.ItemFromID[x]).ToArray();
            ParentId = containers.SingleOrDefault(x => x.HasCategory("FunctionalGroup"))?.ID ?? 0;
        }
    }

    protected override void OnCellChanged(Cell cell)
    {
        base.OnCellChanged(cell);

        switch (cell.Name)
        {
            case "Prop.Quantity":
            {
                if (double.TryParse(Source.Cells["Prop.Quantity"].ResultStr[VisUnitCodes.visUnitsString],
                        out var quantity))
                    Count = quantity;
                break;
            }
        }
    }


    protected override void OnInitialized()
    {
        base.OnInitialized();

        Type = ElementType.Unit;
        ParentId = GetContainerIdByCategory(Source, "FunctionalGroup") ?? 0;
        if (double.TryParse(Source.Cells["Prop.Quantity"].ResultStr[VisUnitCodes.visUnitsString],
                out var value))
            Count = value;
    }

    #endregion
}