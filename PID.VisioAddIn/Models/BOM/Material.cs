﻿using System.Linq;
using System.Text;
using AE.PID.Models.VisProps;
using Microsoft.Office.Interop.Visio;

namespace AE.PID.Models.BOM;

/// <summary>
///     POCO to BOM_template.xlsx
/// </summary>
public class Material
{
    /// <summary>
    ///     The index column.
    /// </summary>
    public int Index { get; set; }

    /// <summary>
    ///     The process zone mapping.
    /// </summary>
    public string ProcessArea { get; set; } = string.Empty;

    /// <summary>
    ///     The functional group mapping.
    /// </summary>
    public string FunctionalGroup { get; set; } = string.Empty;

    /// <summary>
    ///     The function element mapping.
    /// </summary>
    public string? FunctionalElement { get; set; } = string.Empty;

    /// <summary>
    ///     The material no mapping.
    /// </summary>
    public string AEMaterialNo { get; set; } = string.Empty;

    /// <summary>
    ///     The name mapping.
    /// </summary>
    public string NameChinese { get; set; } = string.Empty;

    /// <summary>
    ///     The translation of name in English.
    /// </summary>
    public string NameEnglish { get; set; } = string.Empty;

    /// <summary>
    ///     The merged technical data.
    /// </summary>
    public string TechnicalDataChinese { get; set; } = string.Empty;

    /// <summary>
    ///     The translation of merged technical data.
    /// </summary>
    public string TechnicalDataEnglish { get; set; } = string.Empty;

    /// <summary>
    ///     The quantity of the line item.
    /// </summary>
    public double Count { get; set; }

    /// <summary>
    ///     The sum of the line items of the same technical data within the BOM table.
    /// </summary>
    public double Total { get; set; }

    /// <summary>
    ///     The sum of the line items of the same technical data within the functional group.
    /// </summary>
    public double InGroup { get; set; }

    /// <summary>
    ///     todo:
    /// </summary>
    public string Units { get; set; } = string.Empty;

    /// <summary>
    ///     todo:
    /// </summary>
    public string Manufacturer { get; set; } = string.Empty;

    /// <summary>
    ///     todo:
    /// </summary>
    public string ManufacturerArticleNo { get; set; } = string.Empty;

    /// <summary>
    ///     todo:
    /// </summary>
    public string SerialNo { get; set; } = string.Empty;

    /// <summary>
    ///     todo:
    /// </summary>
    public string Classification { get; set; } = string.Empty;

    /// <summary>
    ///     todo:
    /// </summary>
    public string Attachment { get; set; } = string.Empty;

    /// <summary>
    ///     Converts a base line item to BOM line items.
    ///     If the base item has linked functional elements, these linked functional elements will be flatted as individual bom
    ///     line items.
    /// </summary>
    /// <param name="baseItem"></param>
    /// <returns>A collection of <see cref="Material" /></returns>
    public static Material FromElement(Element baseItem)
    {
        var item = new Material
        {
            ProcessArea = baseItem.ProcessZone,
            FunctionalGroup = baseItem.FunctionalGroup,
            FunctionalElement = baseItem.FunctionalElement,
            AEMaterialNo = baseItem.MaterialNo,
            NameChinese = baseItem.Name,
            Count = baseItem.Count
        };

        var shape = Globals.ThisAddIn.Application.ActivePage.Shapes.OfType<IVShape>()
            .SingleOrDefault(x => x.ID == baseItem.Id);
        if (shape != null && baseItem.Type == ElementType.Single)
            item.TechnicalDataChinese = GetTechnicalData(shape);

        // // flatten linked functional elements if exists
        // if (baseItem.Children != null && baseItem.Children.Any())
        //     items.AddRange(baseItem.Children.SelectMany(FromLineItem));

        return item;
    }

    private static string GetTechnicalData(IVShape shape)
    {
        StringBuilder stringBuilder = new();

        for (var i = 0; i < shape.RowCount[(short)VisSectionIndices.visSectionProp]; i++)
        {
            // skip common properties
            var sort = shape
                .CellsSRC[(short)VisSectionIndices.visSectionProp, (short)i, (short)VisCellIndices.visCustPropsSortKey]
                .ResultStr[VisUnitCodes.visUnitsString];
            if (!string.IsNullOrEmpty(sort)) continue;

            // skip empty value
            var value = shape.CellsSRC[(short)VisSectionIndices.visSectionProp, (short)i,
                (short)VisCellIndices.visCustPropsSortKey].ContainingRow.GetFormatValue();
            if (string.IsNullOrEmpty(value)) continue;

            var label = shape
                .CellsSRC[(short)VisSectionIndices.visSectionProp, (short)i, (short)VisCellIndices.visCustPropsLabel]
                .ResultStr[VisUnitCodes.visUnitsString];

            stringBuilder.Append($"{label}: {value}; ");
        }

        return stringBuilder.ToString();
    }
}