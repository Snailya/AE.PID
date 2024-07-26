using System.Diagnostics.Contracts;
using System.Linq;
using System.Reactive.Disposables;
using AE.PID.Tools;
using Microsoft.Office.Interop.Visio;
using ReactiveUI;

namespace AE.PID.Models;

public sealed class EquipmentUnit : ElementBase
{
    private double _quantity;

    #region Constructors

    public EquipmentUnit(Shape shape) : base(shape)
    {
        Contract.Assert(shape.HasCategory("Unit"),
            "Only shape with category Unit can be construct as EquipmentUnit");
    }

    #endregion

    #region Properties

    public double Quantity
    {
        get => _quantity;
        set => this.SetAndRaise(ref _quantity, value);
    }

    #endregion

    #region Methods Overrides

    protected override void OnRelationshipsChanged(Cell cell)
    {
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

    protected override void OnInitialized()
    {
        Type = ElementType.Unit;
        ParentId = GetContainerIdByCategory(Source, "FunctionalGroup") ?? 0;

        Source.Bind(this, x => x.Quantity, "Prop.Quantity")
            .DisposeWith(CleanUp);
    }

    #endregion
}