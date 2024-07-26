using AE.PID.Attributes;
using AE.PID.Dtos;
using Newtonsoft.Json;

namespace AE.PID.Models;

/// <summary>
///     Because design material from different category might have different properties, use the general property name
///     value pairs to allow arbitrary properties.
/// </summary>
/// <param name="name"></param>
/// <param name="value"></param>
[DataGridColumn(nameof(Name), nameof(Value))]
public class DesignMaterialProperty(string name, string value)
{
    /// <summary>
    ///     The name of the property
    /// </summary>
    [JsonProperty("n")]
    public string Name { get; set; } = name;

    /// <summary>
    ///     The value string
    /// </summary>
    [JsonProperty("v")]
    public string Value { get; set; } = value;

    public static DesignMaterialProperty FromDTO(MaterialPropertyDto dto)
    {
        return new DesignMaterialProperty(dto.Name, dto.Value);
    }
}