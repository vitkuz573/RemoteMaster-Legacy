// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Globalization;
using System.Net;
using System.Net.Sockets;
using RemoteMaster.Host.Core.Abstractions;
using RemoteMaster.Host.Windows.Abstractions;
using RemoteMaster.Host.Windows.Enums;
using Windows.Win32.NetworkManagement.WindowsFirewall;

namespace RemoteMaster.Host.Windows.Services;

public class PsExecService(IHostConfigurationService hostConfigurationService, ICommandExecutor commandExecutor, IFirewallService firewallService) : IPsExecService
{
    private readonly Dictionary<string, string> _ruleGroupNames = new()
    {
        { "en-US", "Remote Service Management" },
        { "ru-RU", "Удаленное управление службой" }
    };

    public async Task EnableAsync()
    {
        var hostConfiguration = await hostConfigurationService.LoadConfigurationAsync(false);
        var ipv4Addrs = await ResolveRemoteAddrsAsync(hostConfiguration.Server);

        await commandExecutor.ExecuteCommandAsync("winrm qc -force");

        if (!string.IsNullOrEmpty(ipv4Addrs))
        {
            firewallService.AddRule("PSExec IPv4", NET_FW_ACTION.NET_FW_ACTION_ALLOW, NET_FW_IP_PROTOCOL.NET_FW_IP_PROTOCOL_TCP, NET_FW_PROFILE_TYPE2.NET_FW_PROFILE2_DOMAIN | NET_FW_PROFILE_TYPE2.NET_FW_PROFILE2_PRIVATE, NET_FW_RULE_DIRECTION.NET_FW_RULE_DIR_IN, InterfaceType.All, "Allow PSExec", @"%WinDir%\system32\services.exe", null, "RPC", ipv4Addrs);
        }

        var localizedRuleGroupName = GetLocalizedRuleGroupName();
        firewallService.EnableRuleGroup(localizedRuleGroupName);
    }

    public void Disable()
    {
        firewallService.RemoveRule("PSExec IPv4");

        var localizedRuleGroupName = GetLocalizedRuleGroupName();
        firewallService.DisableRuleGroup(localizedRuleGroupName);
    }

    private string GetLocalizedRuleGroupName()
    {
        var currentCulture = CultureInfo.CurrentCulture.Name;

        return _ruleGroupNames.TryGetValue(currentCulture, out var localizedGroupName) ? localizedGroupName : _ruleGroupNames["en-US"];
    }

    private static async Task<string> ResolveRemoteAddrsAsync(string server)
    {
        if (string.IsNullOrEmpty(server))
        {
            throw new ArgumentException("Server address cannot be null or empty.", nameof(server));
        }

        if (IsValidIpAddress(server))
        {
            return ConvertToCidrNotation(server);
        }

        if (!IsValidDomainName(server))
        {
            throw new ArgumentException($"Invalid server address format: {server}", nameof(server));
        }

        var addresses = await Dns.GetHostAddressesAsync(server);
        var ipv4Addrs = string.Join(",", addresses.Where(addr => addr.AddressFamily == AddressFamily.InterNetwork).Select(addr => ConvertToCidrNotation(addr.ToString())));

        return ipv4Addrs;
    }

    private static bool IsValidIpAddress(string address)
    {
        return IPAddress.TryParse(address, out _);
    }

    private static bool IsValidDomainName(string domainName)
    {
        return Uri.CheckHostName(domainName) == UriHostNameType.Dns;
    }

    private static string ConvertToCidrNotation(string ipAddress)
    {
        if (!IPAddress.TryParse(ipAddress, out var address))
        {
            return ipAddress;
        }

        return address.AddressFamily == AddressFamily.InterNetwork ? $"{ipAddress}/32" : ipAddress;
    }
}
