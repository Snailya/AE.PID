﻿using System.Collections.Generic;
using System.Linq;
using AE.PID.Attributes;
using AE.PID.Core.DTOs;

namespace AE.PID.Models.BOM;

/// <summary>
///     A design material is used for design bom which might pair to one or many erp material in purchase system.
/// </summary>
/// <param name="name"></param>
/// <param name="categories"></param>
public class DesignMaterial(
    string code,
    string name,
    string brand,
    string specifications,
    string model,
    string unit,
    string manufacturer,
    int[] categories)
{
    /// <summary>
    ///     A design material could belong to several categories on different level, for example, a pump from pump, gear pump
    ///     category.
    /// </summary>
    public int[] Categories { get; set; } = categories;

    /// <summary>
    ///     The extra properties used as technical data
    /// </summary>
    [DataGridColumns]
    public List<DesignMaterialProperty> Properties { get; set; } = [];

    public static DesignMaterial FromDTO(MaterialDto dto)
    {
        return new DesignMaterial(dto.Code, dto.Name, dto.Brand, dto.Specifications, dto.Model, dto.Unit,
            dto.Manufacturer, dto.Categories)
        {
            Properties = dto.Properties == null ? [] : dto.Properties.Select(DesignMaterialProperty.FromDTO).ToList()
        };
    }

    #region General Properties

    /// <summary>
    ///     The human-readable bom code from database, used as identity
    /// </summary>
    [DataGridColumnName("设计物料")]
    public string Code { get; private set; } = code;

    /// <summary>
    ///     The display text of the design material
    /// </summary>
    [DataGridColumnName("名称")]
    public string Name { get; private set; } = name;

    /// <summary>
    ///     The brand of this material. An manufacturer could have many brands.
    /// </summary>
    [DataGridColumnName("品牌")]
    public string Brand { get; set; } = brand;

    [DataGridColumnName("规格")] public string Specifications { get; set; } = specifications;

    [DataGridColumnName("型号")] public string Model { get; set; } = model;

    [DataGridColumnName("单位")] public string Unit { get; set; } = unit;

    [DataGridColumnName("制造商")] public string Manufacturer { get; set; } = manufacturer;

    #endregion
}