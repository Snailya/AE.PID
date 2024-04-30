using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using DynamicData;
using DynamicData.Binding;
using Microsoft.Office.Interop.Visio;
using ReactiveUI;

namespace AE.PID.Models.BOM;

public sealed class FunctionalGroup : FunctionalGroupBase
{
    #region Constructors

    public FunctionalGroup(Shape shape) : base(shape)
    {
        this.WhenAnyValue(x => x.Designation)
            .Select(_ => Unit.Default)
            .Merge(
                Related.ToObservableChangeSet().WhenPropertyChanged(x => x.Designation)
                    .Select(_ => Unit.Default)
            )
            .Subscribe(_ => this.RaisePropertyChanged(nameof(Label)))
            .DisposeWith(CleanUp);
    }

    #endregion

    /// <summary>
    ///     The related proxy functional group.
    /// </summary>
    public ObservableCollection<ProxyFunctionalGroup> Related { get; set; } = [];

    /// <summary>
    ///     Redefine label;
    /// </summary>
    public new string Label => string.Join(",", [Designation, ..Related.Select(x => x.Designation).OrderBy(x => x)]);

    private void UpdateRelatedGroups()
    {
        var current = Source.ContainingPage.Shapes.OfType<Shape>()
            .Where(x => x.HasCategory("Proxy") && x.HasCategory("FunctionalGroup"))
            .Where(x => x.CalloutTarget.ID == Id)
            .ToList();

        var toRemove = Related.Select(x => x.Id).Except(current.Select(x => x.ID)).ToList();
        var toAdd = current.Select(x => x.ID).Except(Related.Select(x => x.Id)).ToList();

        foreach (var proxy in toRemove.Select(remove => Related.Single(x => x.Id == remove)))
        {
            Related.Remove(proxy);
            proxy.Dispose();
        }

        foreach (var add in toAdd) Related.Add(new ProxyFunctionalGroup(current.Single(x => x.ID == add)));

        if (toRemove.Count + toAdd.Count > 0)
            this.RaisePropertyChanged(nameof(Label));
    }

    #region Methods Overrides

    protected override void OnRelationshipsChanged(Cell cell)
    {
        base.OnRelationshipsChanged(cell);

        UpdateRelatedGroups();
    }


    protected override void OnInitialized()
    {
        base.OnInitialized();

        UpdateRelatedGroups();
    }

    #endregion
}