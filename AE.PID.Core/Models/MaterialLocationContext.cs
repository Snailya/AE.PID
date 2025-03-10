using System.ComponentModel.DataAnnotations.Schema;

namespace AE.PID.Core;

/// <summary>
///     物料位置上下文
/// </summary>
[ComplexType]
public record MaterialLocationContext
{
    /// <summary>
    ///     上下文信息：项目的id，特定的项目对所选物料可能会有倾向性
    /// </summary>
    public int? ProjectId { get; set; }

    /// <summary>
    ///     上下文信息：工艺区域编号，同一工艺区域下的设备可能具有共性
    /// </summary>
    public string FunctionZone { get; set; }

    /// <summary>
    ///     上下文信息：功能组编号，同理，同一功能组下的设备可能有关联性
    /// </summary>
    public string FunctionGroup { get; set; }

    /// <summary>
    ///     上下文信息：设备的编号，某些特定编号的设备很可能使用特定的物料
    /// </summary>
    public string FunctionElement { get; set; }

    /// <summary>
    ///     设备的分类，选型与设备类型有关，UserCF模型的Key。
    ///     对于不同的Type，构建不同的UserCF模型。
    /// </summary>
    public string MaterialLocationType { get; set; }

    // /// <summary>
    // ///     相邻对象的MaterialLocationTypes
    // /// </summary>
    // public string[] AdjacentMaterialLocationType { get; set; }
}