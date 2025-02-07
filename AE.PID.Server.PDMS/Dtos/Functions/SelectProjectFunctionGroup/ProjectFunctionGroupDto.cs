using System.Text.Json.Serialization;

namespace AE.PID.Server.PDMS;

public class ProjectFunctionGroupDto : ProjectProcessSectionDto
{
    [JsonPropertyName("projectProcessSection")]
    public string ProjectProcessSection { get; set; }

    [JsonPropertyName("functionNumber")] public string FunctionNumber { get; set; }

    [JsonPropertyName("totalWeight")] public string TotalWeight { get; set; }

    [JsonPropertyName("recordCode")] public string RecordCode { get; set; }
}