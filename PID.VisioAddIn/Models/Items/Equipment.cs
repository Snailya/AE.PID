using System.Diagnostics.Contracts;
using System.Reactive.Disposables;
using AE.PID.Tools;
using Microsoft.Office.Interop.Visio;

namespace AE.PID.Models;

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
        private set => SetAndRaise(ref _subClassName, value);
    }

    #endregion

    #region Methods Overrides

    protected override void OnRelationshipsChanged(Cell cell)
    {
        base.OnRelationshipsChanged(cell);

        ParentId = GetContainerIdByCategory(Source, "Unit") ??
                   GetContainerIdByCategory(Source, "FunctionalGroup") ?? 0;
    }


    public override string GetFunctionalElement()
    {
        return Designation;
    }


    protected override void OnInitialized()
    {
        base.OnInitialized();

        Type = ElementType.Equipment;
        ParentId = GetContainerIdByCategory(Source, "Unit") ??
                   GetContainerIdByCategory(Source, "FunctionalGroup") ?? 0;

        Source.OneWayBind(this, x => x.SubClassName, "Prop.SubClass")
            .DisposeWith(CleanUp);
    }

    #endregion
}