using System;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using AE.PID.Core.Interfaces;
using Microsoft.Office.Interop.Visio;
using ReactiveUI;

namespace AE.PID.Models.BOM;

public abstract class Element : ReactiveObject, IDisposable, ITreeNode
{
    protected readonly CompositeDisposable CleanUp = new();
    protected readonly Shape Source;

    private string _description = string.Empty;
    private string _designation = string.Empty;

    private int _parentId;

    protected static int? GetContainerIdByCategory(Shape shape, string categoryName)
    {
        if (shape.MemberOfContainers.Length == 0) return null;

        var containers = shape.MemberOfContainers
            .OfType<int>()
            .Select(x => shape.ContainingPage.Shapes.ItemFromID[x])
            .ToArray();

        // if it has a unit container, set the parent id to the unit's id
        return containers.SingleOrDefault(x => x.HasCategory(categoryName))?.ID ??
               null;
    }

    #region Public Methods

    /// <summary>
    ///     Highlight the source on page
    /// </summary>
    public void HighlightOnPage()
    {
        Source.ContainingPage.Application.ActiveWindow.Select(Source, (short)VisSelectArgs.visSelect);
        Source.ContainingPage.Application.ActiveWindow.CenterViewOnShape(Source,
            VisCenterViewFlags.visCenterViewSelectShape);
    }

    public virtual void Dispose()
    {
        CleanUp.Dispose();
    }

    #endregion

    #region Constructors

    protected Element(Shape shape)
    {
        Source = shape;
        Id = shape.ID;

        Observable
            .FromEvent<EShape_CellChangedEventHandler, Cell>(
                handler => Source.CellChanged += handler,
                handler => Source.CellChanged -= handler)
            .Subscribe(OnCellChanged)
            .DisposeWith(CleanUp);

        // observable on relationship change
        Observable
            .FromEvent<EShape_FormulaChangedEventHandler, Cell>(
                handler => Source.FormulaChanged += handler,
                handler => Source.FormulaChanged -= handler)
            .Where(cell => cell.Name == "Relationships")
            .Subscribe(OnRelationshipsChanged)
            .DisposeWith(CleanUp);

        Initialize();
    }

    private void Initialize()
    {
        OnInitialized();
    }

    #endregion


    #region Virtual Methods

    protected virtual void OnRelationshipsChanged(Cell cell)
    {
    }

    protected virtual void OnCellChanged(Cell cell)
    {
        Description = cell.Name switch
        {
            "Prop.Description" => cell.ResultStr[VisUnitCodes.visUnitsString],
            _ => Description
        };
    }

    protected virtual void OnInitialized()
    {
        Description = Source.CellsU["Prop.Description"].ResultStr[VisUnitCodes.visUnitsString];
    }

    #endregion

    #region Properties

    public int ParentId
    {
        get => _parentId;
        protected set => this.RaiseAndSetIfChanged(ref _parentId, value);
    }

    public int Id { get; }

    public ElementType Type { get; protected set; }

    /// <summary>
    ///     Notice that the property in visio for designation differs for element type
    /// </summary>
    public string Designation
    {
        get => _designation;
        protected set => this.RaiseAndSetIfChanged(ref _designation, value);
    }

    /// <summary>
    ///     The label is used for binding to TreeListView
    /// </summary>
    public string Label => _designation;

    public string Description
    {
        get => _description;
        protected set => this.RaiseAndSetIfChanged(ref _description, value);
    }

    #endregion
}