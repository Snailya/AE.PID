using System;
using System.Linq;
using System.Net.NetworkInformation;
using Splat;

namespace AE.PID.Client.Infrastructure;

public static class SystemInfoHelper
{
    public static string GetMacAddresses()
    {
        var addresses = string.Empty;

        try
        {
            addresses = string.Join("--",
                NetworkInterface.GetAllNetworkInterfaces().Where(x =>
                        x.NetworkInterfaceType is NetworkInterfaceType.Ethernet or NetworkInterfaceType.Wireless80211 &&
                        !x.Name.Contains("vEthernet"))
                    .Select(x =>
                        BitConverter.ToString(x.GetPhysicalAddress().GetAddressBytes())));


            return addresses;
        }
        catch (Exception e)
        {
            LogHost.Default.Error(e, "Failed to get mac addresses");
        }

        return addresses;
    }
}