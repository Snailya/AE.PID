using AE.PID.Interfaces;
using AE.PID.Models.VisProps;
using Microsoft.Office.Interop.Visio;
using ReactiveUI;

namespace AE.PID.Models.BOM;

public abstract class PartItem : Element, IPartItem
{
    private double _count;
    private DesignMaterial? _designMaterial;
    private string _functionalGroup = string.Empty;
    private string _materialNo = string.Empty;

    #region Constructors

    protected PartItem(Shape shape) : base(shape)
    {
    }

    #endregion

    #region Public Methods

    public string GetTechnicalData()
    {
        // todo: implement

        return string.Empty;
    }

    #endregion

    #region Abstract Methods

    public abstract string GetFunctionalElement();
    public abstract string GetName();

    #endregion

    #region Methods Overrides

    protected override void Initialize()
    {
        base.Initialize();

        FunctionalGroup = Source.TryGetFormatValue("Prop.FunctionalGroup") ?? string.Empty;
        MaterialNo = Source.TryGetFormatValue("Prop.D_BOM") ?? string.Empty;
        if (double.TryParse(Source.Cells["Prop.Subtotal"].ResultStr[VisUnitCodes.visUnitsString],
                out var value))
            Count = value;
    }

    protected override void OnCellChanged(Cell cell)
    {
        base.OnCellChanged(cell);

        switch (cell.Name)
        {
            // bind FunctionalGroup to Prop.FunctionalGroup
            case "Prop.FunctionalGroup":
                FunctionalGroup = Source.TryGetFormatValue("Prop.FunctionalGroup") ?? string.Empty;
                break;
            // bind Description to Prop.Description
            case "Prop.FunctionalElement":
                Designation = Source.TryGetFormatValue("Prop.FunctionalElement") ?? string.Empty;
                this.RaisePropertyChanged(nameof(Label));
                break;

            // bind Description to Prop.FunctionalGroup
            case "Prop.Subtotal":
                if (double.TryParse(Source.Cells["Prop.Subtotal"].ResultStr[VisUnitCodes.visUnitsString],
                        out var subtotal))
                    Count = subtotal;
                break;
            case "Prop.D_BOM":
                MaterialNo = Source.TryGetFormatValue("Prop.D_BOM") ?? string.Empty;
                break;
        }
    }

    #endregion

    #region Properties

    public DesignMaterial? DesignMaterial
    {
        get => _designMaterial;
        set
        {
            this.RaiseAndSetIfChanged(ref _designMaterial, value);
            AssignMaterial(_designMaterial);
        }
    }

    public string FunctionalGroup
    {
        get => _functionalGroup;
        set => this.RaiseAndSetIfChanged(ref _functionalGroup, value);
    }

    public string MaterialNo
    {
        get => _materialNo;
        set => this.RaiseAndSetIfChanged(ref _materialNo, value);
    }

    public double Count
    {
        get => _count;
        set => this.RaiseAndSetIfChanged(ref _count, value);
    }

    #endregion
}