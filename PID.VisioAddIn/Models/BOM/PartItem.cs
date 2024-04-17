using System.Linq;
using AE.PID.Interfaces;
using AE.PID.Models.VisProps;
using AE.PID.Tools;
using Microsoft.Office.Interop.Visio;
using ReactiveUI;

namespace AE.PID.Models.BOM;

public abstract class PartItem(Shape shape) : Element(shape), IPartItem
{
    private double _count;
    private DesignMaterial? _designMaterial;
    private string _functionalGroup = string.Empty;
    private string _materialNo = string.Empty;
    private string _keyParameters = string.Empty;
    
    /// <summary>
    ///     Write material to the shape sheet.
    /// </summary>
    /// <param name="material"></param>
    private void AssignMaterial(DesignMaterial? material)
    {
        DeleteMaterial();

        // write material id
        if (material == null) return;

        var shapeData = new ShapeData("D_BOM", "\"设计物料\"", "", $"\"{material.Code}\"");
        Source.AddOrUpdate(shapeData);

        // write related properties
        foreach (var propertyData in from property in material.Properties
                 let rowName = $"D_Attribute{material.Properties.IndexOf(property) + 1}"
                 select new ShapeData(rowName, $"\"{property.Name}\"", "",
                     $"\"{property.Value.Replace("\"", "\"\"")}\""))
            Source.AddOrUpdate(propertyData);
    }


    /// <summary>
    /// Remove all property that start with D_ from shape sheet.
    /// </summary>
    private void DeleteMaterial()
    {
        // clear all shape data starts with D_BOM
        for (var i = Source.RowCount[(short)VisSectionIndices.visSectionProp] - 1; i >= 0; i--)
        {
            var cell = Source.CellsSRC[(short)VisSectionIndices.visSectionProp, (short)i,
                (short)VisCellIndices.visCustPropsValue];
            if (cell.RowName.StartsWith("D_")) Source.DeleteRow((short)VisSectionIndices.visSectionProp, (short)i);
        }
    }


    private void CopyMaterialFrom(int sourceId)
    {
        var materialSource = Source.ContainingPage.Shapes.ItemFromID[sourceId];

        for (var i = 0; i < materialSource.RowCount[(short)VisSectionIndices.visSectionProp]; i++)
        {
            var valueCell = materialSource.CellsSRC[(short)VisSectionIndices.visSectionProp, (short)i,
                (short)VisCellIndices.visCustPropsValue];
            if (!valueCell.RowName.StartsWith("D_")) continue;

            var labelCell = materialSource.CellsSRC[(short)VisSectionIndices.visSectionProp, (short)i,
                (short)VisCellIndices.visCustPropsLabel];

            var shapeData =
                new ShapeData(valueCell.RowName, labelCell.FormulaU, "",
                    valueCell.FormulaU);
            Source.AddOrUpdate(shapeData);
        }
    }

    #region Public Methods

    public string GetTechnicalData()
    {
        // todo: implement

        return string.Empty;
    }

    /// <summary>
    ///     Copy material properties from other.
    /// </summary>
    /// <param name="partItem"></param>
    public void CopyMaterialFrom(PartItem partItem)
    {
        // delete previous material
        DeleteMaterial();

        // copy
        if (partItem.DesignMaterial != null) DesignMaterial = partItem.DesignMaterial;
        else
            CopyMaterialFrom(partItem.Id);
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
        KeyParameters = Source.Cells["User.KeyParameters"].ResultStr[VisUnitCodes.visUnitsString];
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
            case "User.KeyParameters":
                KeyParameters = Source.Cells["User.KeyParameters"].ResultStr[VisUnitCodes.visUnitsString];
                break;
        }
    }

    #endregion

    #region Properties

    public string KeyParameters
    {
        get => _keyParameters;
        set => this.RaiseAndSetIfChanged(ref _keyParameters, value);
    }

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
        protected set => this.RaiseAndSetIfChanged(ref _materialNo, value);
    }

    public double Count
    {
        get => _count;
        protected set => this.RaiseAndSetIfChanged(ref _count, value);
    }

    #endregion
}