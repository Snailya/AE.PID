using System;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using AE.PID.Interfaces;
using AE.PID.Services;
using AE.PID.Tools;
using Microsoft.Office.Interop.Visio;
using ReactiveUI;
using Splat;

namespace AE.PID.Models;

public abstract class PartItem(Shape shape) : ElementBase(shape), IPartItem
{
    private DesignMaterial? _designMaterial;
    private string _functionalGroup = string.Empty;
    private string _keyParameters = string.Empty;
    private string _materialNo = string.Empty;
    private double _quantity;
    private double _subTotal;

    /// <summary>
    ///     Write material to the shape sheet.
    /// </summary>
    /// <param name="material"></param>
    private void AssignMaterial(DesignMaterial? material)
    {
        // write material id
        if (material == null)
        {
            VisioHelper.DeleteDesignMaterial(Source);
            return;
        }

        var shapeData = new ShapeData("D_BOM", "设计物料", "", $"{material.Code}");
        Source.CreateOrUpdate(shapeData);

        // remove attributes
        for (var i = Source.RowCount[(short)VisSectionIndices.visSectionProp] - 1; i >= 0; i--)
        {
            var cell = Source.CellsSRC[(short)VisSectionIndices.visSectionProp, (short)i,
                (short)VisCellIndices.visCustPropsValue];
            if (cell.RowName.StartsWith("D_Attribute"))
                Source.DeleteRow((short)VisSectionIndices.visSectionProp, (short)i);
        }

        // write related properties
        foreach (var propertyData in from property in material.Properties
                 let rowName = $"D_Attribute{material.Properties.IndexOf(property) + 1}"
                 select new ShapeData(rowName, $"{property.Name}", "",
                     $"{property.Value.Replace("\"", "\"\"")}"))
            Source.CreateOrUpdate(propertyData);
    }

    private void AssignMaterial(string code)
    {
        var service = Locator.Current.GetService<MaterialsService>()!;
        var material = service.Materials.SingleOrDefault(x => x.Code == code);
        AssignMaterial(material);
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
            Source.CreateOrUpdate(shapeData);
        }
    }

    #region Methods Overrides

    protected override void OnInitialized()
    {
        Source.OneWayBind(this, x => x.FunctionalGroup, "Prop.FunctionalGroup")
            .DisposeWith(CleanUp);
        Source.Bind(this, x => x.Designation, "Prop.FunctionalElement")
            .DisposeWith(CleanUp);
        Source.OneWayBind(this, x => x.MaterialNo, "Prop.D_BOM")
            .DisposeWith(CleanUp);
        Source.Bind(this, x => x.Description, "Prop.Description")
            .DisposeWith(CleanUp);
        Source.OneWayBind(this, x => x.KeyParameters, "User.KeyParameters")
            .DisposeWith(CleanUp);
        Source.Bind(this, x => x.Quantity, "Prop.Quantity", s => double.TryParse(s, out var quantity) ? quantity : 0)
            .DisposeWith(CleanUp);
        Source.OneWayBind(this, x => x.SubTotal, "Prop.Subtotal")
            .DisposeWith(CleanUp);

        this.WhenAnyValue(x => x.MaterialNo)
            .DistinctUntilChanged()
            .ObserveOn(ThisAddIn.Dispatcher!)
            .Subscribe(AssignMaterial)
            .DisposeWith(CleanUp);
    }

    #endregion

    #region Public Methods

    public string GetTechnicalData()
    {
        // todo: implement

        return string.Empty;
    }

    /// <summary>
    ///     Copy material properties from another part item.
    /// </summary>
    /// <param name="partItem"></param>
    public void CopyMaterialFrom(PartItem partItem)
    {
        // delete previous material
        VisioHelper.DeleteDesignMaterial(Source);

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

    #region Properties

    public string KeyParameters
    {
        get => _keyParameters;
        set => this.RaiseAndSetIfChanged(ref _keyParameters, value);
    }

    public DesignMaterial? DesignMaterial
    {
        get => _designMaterial;
        set => this.RaiseAndSetIfChanged(ref _designMaterial, value);
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

    public double Quantity
    {
        get => _quantity;
        set => this.RaiseAndSetIfChanged(ref _quantity, value);
    }

    public double SubTotal
    {
        get => _subTotal;
        protected set => this.RaiseAndSetIfChanged(ref _subTotal, value);
    }

    #endregion
}