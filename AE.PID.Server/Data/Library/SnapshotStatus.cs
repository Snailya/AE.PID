using System.ComponentModel;
using System.Text.Json.Serialization;

namespace AE.PID.Server.Data;

[JsonConverter(typeof(JsonStringEnumConverter<SnapshotStatus>))]
public enum SnapshotStatus
{
    [Description("废弃")]
    Obsolete = -1,
    [Description("草稿")]
    Draft = 0,
    [Description("发布")]
    Published = 1
}