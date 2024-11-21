using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using AE.PID.Core.Models;
using AE.PID.Visio.Core.Interfaces;
using AE.PID.Visio.Core.Models;
using AE.PID.Visio.UI.Design.Design;
using DynamicData;

namespace AE.PID.Visio.UI.Design.Services;

public class MoqVisioService : IVisioService
{
    private readonly SourceCache<VisioMaster, string> _masters = new(x => x.BaseId);
    private readonly SourceCache<VisioShape, CompositeId> _shapes = new(x => x.Id);

    public MoqVisioService()
    {
        Shapes = new Lazy<IObservableCache<VisioShape, CompositeId>>(() =>
        {
            // add initial data
            _shapes.AddOrUpdate(
                DesignData.Shapes.Select(x => new VisioShape(new CompositeId(1, x.Id), ResolveShapeTypes(x))));
            return _shapes.AsObservableCache();
        });

        Masters = new Lazy<IObservableCache<VisioMaster, string>>(() => _masters.AsObservableCache());
    }

    public CompositeId[] GetAdjacent(CompositeId compositeId)
    {
        return [new CompositeId(compositeId.PageId, 10)];
    }

    public Lazy<IObservableCache<VisioMaster, string>> Masters { get; }

    public FunctionLocation ToFunctionLocation(VisioShape shape)
    {
        var source = DesignData.Shapes.SingleOrDefault(x => x.Id == shape.Id.ShapeId);
        if (source != null)
            return new FunctionLocation(shape.Id, ResolveFunctionType(source))
            {
                Zone = source.Zone,
                ZoneName = source.ZoneName,
                ZoneEnglishName = source.ZoneNameEnglish,
                Group = source.Group,
                GroupName = source.GroupName,
                GroupEnglishName = source.GroupNameEnglish,
                Element = source.Element,
                Description = source.Description,
                Name = "",
                Remarks = source.Remarks,
                FunctionId = source.PDMSFunctionId,
                ParentId = new CompositeId(1, source.ParentId)
            };

        throw new ArgumentOutOfRangeException(nameof(shape));
    }

    public MaterialLocation ToMaterialLocation(VisioShape shape)
    {
        var source = DesignData.Shapes.SingleOrDefault(x => x.Id == shape.Id.ShapeId);
        if (source != null)
            return new MaterialLocation(shape.Id)
            {
                Code = source.MaterialCode,
                Quantity = 1,
                ComputedQuantity = 1,
                KeyParameters = "",
                Category = source.MaterialType
            };

        throw new ArgumentOutOfRangeException(nameof(shape));
    }

    public void SelectAndCenterView(CompositeId id)
    {
        Debug.WriteLine($"Located {id}");
    }

    public string? GetDocumentProperty(string propName)
    {
        return DesignData.DocumentSheet.GetValueOrDefault(propName);
    }

    public string? GetPageProperty(int id, string propName)
    {
        return DesignData.PageSheet.GetValueOrDefault((id, propName));
    }

    public string? GetShapeProperty(CompositeId id, string propName)
    {
        throw new NotImplementedException();
    }

    public void UpdateDocumentProperties(IEnumerable<ValuePatch> patches)
    {
        foreach (var patch in patches)
            DesignData.DocumentSheet[patch.PropertyName] = patch.Value.ToString() ?? string.Empty;
    }

    public void UpdatePageProperties(int id, IEnumerable<ValuePatch> patches)
    {
        foreach (var patch in patches)
            DesignData.PageSheet[(id, patch.PropertyName)] = patch.Value?.ToString() ?? string.Empty;
    }

    public void UpdateShapeProperties(CompositeId id, IEnumerable<ValuePatch> patches)
    {
        Debug.WriteLine("Updated");
    }

    public Lazy<IObservableCache<VisioShape, CompositeId>> Shapes { get; }

    public void InsertAsExcelSheet(string[,] dataArray)
    {
        Debug.WriteLine("Inserted");
    }

    private static VisioShape.ShapeType[] ResolveShapeTypes(ShapeProxy shape)
    {
        if (string.IsNullOrEmpty(shape.ShapeCategory)) return [VisioShape.ShapeType.None];

        return shape.ShapeCategory switch
        {
            "Frame" or "FunctionalGroup" or "Unit" => [VisioShape.ShapeType.FunctionLocation],
            "Equipment" or "Instrument" or "FunctionalElement" =>
            [
                VisioShape.ShapeType.FunctionLocation, VisioShape.ShapeType.MaterialLocation
            ],
            _ => [VisioShape.ShapeType.None]
        };
    }

    private static FunctionType ResolveFunctionType(ShapeProxy shape)
    {
        return shape.ShapeCategory switch
        {
            "Frame" => FunctionType.ProcessZone,
            "FunctionalGroup" => FunctionType.FunctionGroup,
            "Unit" => FunctionType.FunctionUnit,
            "Equipment" => FunctionType.Equipment,
            "Instrument" => FunctionType.Instrument,
            "FunctionalElement" => FunctionType.FunctionElement,
            _ => throw new ArgumentOutOfRangeException()
        };
    }
}