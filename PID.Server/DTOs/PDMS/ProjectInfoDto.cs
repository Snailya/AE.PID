using System.Text.Json.Serialization;

namespace AE.PID.Server.DTOs.PDMS;

public class ProjectInfoDto
{
    [JsonPropertyName("id")] public string Id { get; set; }

    /// <summary>
    /// 编号
    /// </summary>
    [JsonPropertyName("projectCode")]
    public string ProjectCode { get; set; }

    /// <summary>
    /// 名称
    /// </summary>
    [JsonPropertyName("projectName")]
    public string ProjectName { get; set; }

    /// <summary>
    /// 项目状态
    /// </summary>
    [JsonPropertyName("statusId")]
    public string StatusId { get; set; }

    /// <summary>
    /// 项目类别
    /// </summary>
    [JsonPropertyName("familyId")]
    public string FamilyId { get; set; }
}