using System;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Threading.Tasks;
using AE.PID.Interfaces;
using AE.PID.Services;
using AE.PID.Tools;
using Microsoft.Office.Interop.Visio;
using Newtonsoft.Json;
using Splat;

namespace AE.PID.Models;

public abstract class PartItem : ElementBase, IPartItem
{
    private string _functionalGroup = string.Empty;
    private string _keyParameters = string.Empty;
    private string _materialNo = string.Empty;
    private double _quantity;
    private double _subTotal;

    protected PartItem(Shape shape) : base(shape)
    {
        DesignMaterial = new Lazy<DesignMaterial?>(() =>
            JsonConvert.DeserializeObject<DesignMaterial>(Source.Data1)
        );
    }

    /// <summary>
    ///     Write material to the shape sheet.
    /// </summary>
    /// <param name="material"></param>
    public void AssignMaterial(DesignMaterial? material)
    {
        // if the material to assign is null, delete the properties starts with D_ and Data1
        if (material == null)
        {
            VisioHelper.DeleteDesignMaterial(Source);
            return;
        }

        // update teh material no.
        MaterialNo = material.MaterialNo;
        var shapeData = new ShapeData("D_BOM", "设计物料", "", $"{material.MaterialNo}");
        Source.CreateOrUpdate(shapeData);

        // write serialized data to Data1
        var data = JsonConvert.SerializeObject(material);
        if (data.Length <= 3000) Source.Data1 = data;
        else LogHost.Default.Warn($"Material data length exceeds 3000 {material.MaterialNo}");

        // rewrite design material properties as D_Attribute
        for (var i = Source.RowCount[(short)VisSectionIndices.visSectionProp] - 1; i >= 0; i--)
        {
            var cell = Source.CellsSRC[(short)VisSectionIndices.visSectionProp, (short)i,
                (short)VisCellIndices.visCustPropsValue];
            if (cell.RowName.StartsWith("D_Attribute"))
                Source.DeleteRow((short)VisSectionIndices.visSectionProp, (short)i);
        }

        foreach (var propertyData in from property in material.Properties
                 let rowName = $"D_Attribute{material.Properties.IndexOf(property) + 1}"
                 select new ShapeData(rowName, $"{property.Name}", "",
                     $"{property.Value.Replace("\"", "\"\"")}"))
            Source.CreateOrUpdate(propertyData);
    }

    #region Methods Overrides

    protected override void OnInitialized()
    {
        base.OnInitialized();

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


        if (MaterialNo != string.Empty && Source.Data1 == string.Empty)
            Task.Run(async () =>
            {
                var material = await Locator.Current.GetService<MaterialsService>()!.GetMaterialByCode(MaterialNo);
                AppScheduler.VisioScheduler.Schedule(() => { Source.Data1 = JsonConvert.SerializeObject(material); });
            });
    }

    #endregion

    #region Public Methods

    /// <summary>
    ///     Copy material properties from another part item.
    /// </summary>
    /// <param name="partItem"></param>
    public void CopyMaterialFrom(PartItem partItem)
    {
        // delete previous material
        VisioHelper.DeleteDesignMaterial(Source);
        AssignMaterial(partItem.DesignMaterial.Value);
    }

    #endregion

    #region Abstract Methods

    public abstract string GetFunctionalElement();

    #endregion


    #region Properties

    public string KeyParameters
    {
        get => _keyParameters;
        set => SetAndRaise(ref _keyParameters, value);
    }

    public readonly Lazy<DesignMaterial?> DesignMaterial;

    public string FunctionalGroup
    {
        get => _functionalGroup;
        set => SetAndRaise(ref _functionalGroup, value);
    }

    public string MaterialNo
    {
        get => _materialNo;
        set => SetAndRaise(ref _materialNo, value);
    }

    public double Quantity
    {
        get => _quantity;
        set => SetAndRaise(ref _quantity, value);
    }

    public double SubTotal
    {
        get => _subTotal;
        protected set => SetAndRaise(ref _subTotal, value);
    }

    #endregion
}