using System.Diagnostics.Contracts;
using System.Reactive.Disposables;
using AE.PID.Tools;
using Microsoft.Office.Interop.Visio;
using ReactiveUI;

namespace AE.PID.Models.BOM;

public sealed class Instrument : Equipment
{
    private string _processVariableAndControlFunctions = string.Empty;

    #region Constructors

    public Instrument(Shape shape) : base(shape)
    {
        Contract.Assert(shape.HasCategory("Instrument"),
            "Only shape with category Instrument can be construct as Instrument");
    }

    #endregion

    #region Properties

    public string ProcessVariableAndControlFunctions
    {
        get => _processVariableAndControlFunctions;
        set => this.RaiseAndSetIfChanged(ref _processVariableAndControlFunctions, value);
    }

    #endregion

    #region Methods Overrides

    protected override void OnInitialized()
    {
        base.OnInitialized();

        Type = ElementType.Instrument;

        Source.Bind(this, x => x.ProcessVariableAndControlFunctions, "Prop.ProcessVariableAndControlFunctions")
            .DisposeWith(CleanUp);
    }

    #endregion
}