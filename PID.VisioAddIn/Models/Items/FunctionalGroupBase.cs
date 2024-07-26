using System;
using System.Diagnostics.Contracts;
using System.Reactive.Disposables;
using AE.PID.Tools;
using DynamicData.Binding;
using Microsoft.Office.Interop.Visio;
using ReactiveUI;

namespace AE.PID.Models;

public abstract class FunctionalGroupBase : ElementBase
{
    #region Constructors

    protected FunctionalGroupBase(Shape shape) : base(shape)
    {
        Contract.Assert(shape.HasCategory("FunctionalGroup"),
            "Only shape with category FunctionalGroup can be construct as FunctionalGroup");
    }

    #endregion

    #region Methods Overrides

    protected override void OnInitialized()
    {
        base.OnInitialized();

        Type = ElementType.FunctionalGroup;
        ParentId = 0;
        
        Source.Bind(this, x => x.Designation, "Prop.FunctionalGroup")
            .DisposeWith(CleanUp);
        Source.Bind(this, x => x.Description, "Prop.FunctionalGroupName")
            .DisposeWith(CleanUp);

        this.WhenPropertyChanged(x => x.Description)
            .Subscribe(_ => OnPropertyChanged(nameof(Label)))
            .DisposeWith(CleanUp);
    }

    #endregion
}