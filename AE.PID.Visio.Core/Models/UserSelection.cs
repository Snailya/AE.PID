namespace AE.PID.Visio.Core.Models;

public class SelectionFeedback
{
    /// <summary>
    ///     工艺区域编号，同一工艺区域下的设备可能具有共性
    /// </summary>
    public string FunctionZone { get; set; }

    /// <summary>
    ///     功能组编号，同理，同一功能组下的设备可能有关联性
    /// </summary>
    public string FunctionGroup { get; set; }

    /// <summary>
    ///     设备的编号，某些特定编号的设备很可能使用特定的物料
    /// </summary>
    public string FunctionElement { get; set; }

    /// <summary>
    ///     设备的分类，选型与设备类型有关
    /// </summary>
    public string MaterialLocationType { get; set; }

    /// <summary>
    ///     选择的物料号
    /// </summary>
    public string MaterialCode { get; set; }
    
    /// <summary>
    /// 向用户推荐的物料的code，用以获得转化率，以评价模型。
    /// </summary>
    public string RecommendMaterialCode { get; set; }

    /// <summary>
    /// 生成推荐物料的模型，用以在AB测试中评估模型。
    /// </summary>
    public string RecommendModel{get;set;} 
}