using System.ComponentModel;
using System.Text.Json.Serialization;

namespace AE.PID.Core;

[JsonConverter(typeof(JsonStringEnumConverter<VersionChannel>))]
public enum VersionChannel
{
    [Description("开发内部测试版本")] InternalTesting = 10,
    [Description("限量用户灰度测试")] LimitedBeta = 20,
    [Description("公开可用版本")] GeneralAvailability = 30
}