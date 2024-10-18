using System.Management;

namespace AE.PID.Visio.Shared;

public static class SystemInfoHelper
{
    public static string GetUUID()
    {
        var deviceId = string.Empty;
        var mc = new ManagementClass("Win32_ComputerSystemProduct");
        var moc = mc.GetInstances();

        foreach (var mo in moc)
        {
            deviceId = mo.Properties["UUID"].Value.ToString();
            break;
        }

        return deviceId;
    }
}