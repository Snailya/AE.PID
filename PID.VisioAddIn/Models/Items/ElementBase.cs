using System;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using AE.PID.Interfaces;
using AE.PID.Tools;
using DynamicData.Binding;
using Microsoft.Office.Interop.Visio;
using ReactiveUI;

namespace AE.PID.Models;

public abstract class ElementBase : AbstractNotifyPropertyChanged, IDisposable, ITreeNode, IEquatable<ElementBase>
{
    protected readonly CompositeDisposable CleanUp = new();
    protected readonly Shape Source;

    private string _description = string.Empty;
    private string _designation = string.Empty;

    private int _parentId;
    private string _processArea = string.Empty;

    #region Constructors

    protected ElementBase(Shape shape)
    {
        Source = shape;
        Id = shape.ID;

        // setup initial value
        Initialize();

        // observe on relationship change
        Observable
            .FromEvent<EShape_FormulaChangedEventHandler, Cell>(
                handler => Source.FormulaChanged += handler,
                handler => Source.FormulaChanged -= handler)
            .Where(cell => cell.Name == "Relationships")
            .Subscribe(OnRelationshipsChanged)
            .DisposeWith(CleanUp);
    }

    #endregion

    public bool Equals(ElementBase other)
    {
        return Id == other.Id;
    }

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
    ///     Select and make it view center.
    /// </summary>
    public void Select()
    {
        Source.ContainingPage.Application.ActiveWindow.Select(Source, (short)VisSelectArgs.visSelect);
        Source.ContainingPage.Application.ActiveWindow.CenterViewOnShape(Source,
            VisCenterViewFlags.visCenterViewSelectShape);
    }

    #endregion

    private void Initialize()
    {
        OnInitialized();
    }

    #region Virtual Methods

    protected virtual void OnRelationshipsChanged(Cell cell)
    {
        //
    }

    protected virtual void OnInitialized()
    {
        Source.OneWayBind<ElementBase, string>(this, x => x.ProcessArea, "Prop.ProcessZone")
            .DisposeWith(CleanUp);
    }

    public virtual void Dispose()
    {
        CleanUp.Dispose();
    }

    #endregion

    #region Properties

    /// <summary>
    ///     The shape id of the logical parent.
    /// </summary>
    public int ParentId
    {
        get => _parentId;
        protected set => this.SetAndRaise(ref _parentId, value);
    }

    /// <summary>
    ///     The shape id of the source shape.
    /// </summary>
    public int Id { get; }

    public ElementType Type { get; protected set; }

    /// <summary>
    ///     Notice that the property in visio for designation differs for an element type.
    /// </summary>
    public string Designation
    {
        get => _designation;
        set => this.SetAndRaise(ref _designation, value);
    }

    /// <summary>
    ///     The label is used for binding to TreeListView
    /// </summary>
    public string Label => _designation;

    /// <summary>
    ///     The description of this element. For part item, it maps from Prop.Description. For functional group, it maps from
    ///     Prop.FunctionalGroupDescription.
    /// </summary>
    public string Description
    {
        get => _description;
        set => SetAndRaise(ref _description, value);
    }

    public string ProcessArea
    {
        get => _processArea;
        set => SetAndRaise(ref _processArea, value);
    }

    #endregion
}

public enum ElementType
{
    FunctionalGroup,
    Unit,
    Equipment,
    Instrument,
    FunctionalElement
}