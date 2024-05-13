namespace AE.PID.Models;

public class PartListTableLineItem
{
    /// <summary>
    ///     Create a <see cref="PartListTableLineItem" /> from <see cref="PartItem" />.
    /// </summary>
    /// <param name="x"></param>
    /// <returns></returns>
    public static PartListTableLineItem FromPartItem(PartItem x)
    {
        return new PartListTableLineItem
        {
            ProcessArea = null,
            FunctionalGroup = x.FunctionalGroup,
            FunctionalElement = x.GetFunctionalElement(),
            AEMaterialNo = x.MaterialNo,
            NameChinese = x.GetName(),
            NameEnglish = null,
            Description = x.Description,
            TechnicalDataChinese = x.GetTechnicalData(),
            TechnicalDataEnglish = null,
            Count = x.SubTotal,
            Total = x.SubTotal,
            InGroup = x.SubTotal,
            Units = x.DesignMaterial?.Unit ?? string.Empty,
            Manufacturer = x.DesignMaterial?.Manufacturer ?? string.Empty,
            ManufacturerArticleNo = x.DesignMaterial?.ManufacturerMaterialNumber ?? string.Empty,
            SerialNo = null,
            Classification = null,
            Attachment = null
        };
    }

    /// <summary>
    ///     Create a copy of <see cref="PartListTableLineItem" /> and reset its designations.
    ///     Used for virtual part items.
    /// </summary>
    /// <param name="x"></param>
    /// <param name="targetFunctionalGroup"></param>
    /// <returns></returns>
    public static PartListTableLineItem CopyTo(PartListTableLineItem x, string targetFunctionalGroup)
    {
        return new PartListTableLineItem
        {
            ProcessArea = x.ProcessArea,
            FunctionalGroup = targetFunctionalGroup,
            FunctionalElement = x.FunctionalElement.Replace(x.FunctionalGroup, targetFunctionalGroup),
            AEMaterialNo = x.AEMaterialNo,
            NameChinese = x.NameChinese,
            NameEnglish = x.NameEnglish,
            TechnicalDataChinese = x.TechnicalDataChinese,
            TechnicalDataEnglish = x.TechnicalDataEnglish,
            Count = x.Count,
            Total = x.Total,
            InGroup = x.InGroup,
            Units = x.Units,
            Manufacturer = x.Manufacturer,
            ManufacturerArticleNo = x.ManufacturerArticleNo,
            SerialNo = x.SerialNo,
            Classification = x.Classification,
            Attachment = x.Attachment,
            Description = x.Description
        };
    }

    #region Properties

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
    public string FunctionalElement { get; set; } = string.Empty;

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
    ///     The description of the part.
    /// </summary>
    public string Description { get; set; } = string.Empty;

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

    #endregion
}