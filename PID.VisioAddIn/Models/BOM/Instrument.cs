using System.Diagnostics.Contracts;
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
        ProcessVariableAndControlFunctions =
            Source.TryGetFormatValue("Prop.ProcessVariableAndControlFunctions") ?? string.Empty;
    }

    protected override void OnCellChanged(Cell cell)
    {
        base.OnCellChanged(cell);

        switch (cell.Name)
        {
            // bind Description to Prop.Description
            case "Prop.ProcessVariableAndControlFunctions":
                ProcessVariableAndControlFunctions =
                    Source.TryGetFormatValue("Prop.ProcessVariableAndControlFunctions") ?? string.Empty;
                break;
        }
    }

    #endregion
}