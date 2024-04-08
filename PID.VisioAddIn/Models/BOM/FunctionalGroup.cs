using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using AE.PID.Models.VisProps;
using Microsoft.Office.Interop.Visio;
using ReactiveUI;

namespace AE.PID.Models.BOM;

public sealed class FunctionalGroup : Element
{
    private IEnumerable<string> _related = [];

    #region Constructors

    public FunctionalGroup(Shape shape) : base(shape)
    {
        Contract.Assert(shape.HasCategory("FunctionalGroup"),
            "Only shape with category FunctionalGroup can be construct as FunctionalGroup");

        Initialize();
    }

    #endregion

    #region Properties

    public IEnumerable<string> Related
    {
        get => _related;
        private set => this.RaiseAndSetIfChanged(ref _related, value);
    }

    #endregion

    /// <summary>
    ///     Redefine label;
    /// </summary>
    public new string Label => string.Join(",", [Designation, ..Related]);

    private List<string> GetRelated()
    {
        var relatedSources = Source.ContainingPage.Shapes.OfType<Shape>()
            .Where(x => x.HasCategory("Proxy") && x.HasCategory("FunctionalGroup")).Where(x => x.CalloutTarget.ID == Id)
            .ToList();
        if (!relatedSources.Any()) return [];

        var related = relatedSources.Select(x => x.TryGetFormatValue("Prop.FunctionalGroup") ?? string.Empty)
            .OrderBy(x => x).ToList();
        return related;
    }

    #region Methods Overrides

    protected override void OnRelationshipsChanged()
    {
        base.OnRelationshipsChanged();

        var related = GetRelated();
        if (related.SequenceEqual(Related)) return;

        Related = related;
        this.RaisePropertyChanged(nameof(Label));
    }

    protected override void OnCellChanged(Cell cell)
    {
        base.OnCellChanged(cell);

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

    protected override void Initialize()
    {
        Type = ElementType.FunctionalGroup;
        ParentId = 0;
        Designation = Source.CellsU["Prop.FunctionalGroup"].ResultStr[VisUnitCodes.visUnitsString];
        Description = Source.CellsU["Prop.FunctionalGroupDescription"].ResultStr[VisUnitCodes.visUnitsString];
        var related = GetRelated();
        if (!related.SequenceEqual(Related)) Related = related;
    }

    #endregion
}