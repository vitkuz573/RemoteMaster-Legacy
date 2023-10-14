using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using RemoteMaster.Host.Core.Abstractions;
using RemoteMaster.Host.Core.Extensions;

namespace RemoteMaster.Host.Core.Services;

public partial class HostInfoService : IHostInfoService
{
    public string GetHostName() => Dns.GetHostName();

    public string GetIPv4Address()
    {
        var hostName = GetHostName();
        var ipv4Address = Dns.GetHostAddresses(hostName)
                            .FirstOrDefault(a => a.AddressFamily == AddressFamily.InterNetwork);

        return ipv4Address == null ? throw new InvalidOperationException("IPv4 address not found") : ipv4Address.ToString();
    }

    public string GetMacAddress()
    {
        var ipv4Address = GetIPv4Address();

        var targetInterface = NetworkInterface.GetAllNetworkInterfaces()
            .FirstOrDefault(nic => nic.GetIPProperties()
            .UnicastAddresses
            .Any(address => address.Address.ToString() == ipv4Address))
            ?? throw new InvalidOperationException("MAC address not found");

        return targetInterface.GetMacAddress();
    }
}
