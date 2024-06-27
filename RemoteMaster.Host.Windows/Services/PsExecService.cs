// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Globalization;
using RemoteMaster.Host.Core.Abstractions;
using RemoteMaster.Host.Windows.Abstractions;

namespace RemoteMaster.Host.Windows.Services;

public class PsExecService(IHostConfigurationService hostConfigurationService, ICommandExecutor commandExecutor) : IPsExecService
{
    private readonly Dictionary<string, string> _ruleGroupNames = new()
        {
            { "en-US", "Remote Service Management" },
            { "ru-RU", "Удаленное управление службой" }
        };

    public async Task EnableAsync()
    {
        var hostConfiguration = await hostConfigurationService.LoadConfigurationAsync(false);

        await commandExecutor.ExecuteCommandAsync("winrm qc -force");
        await commandExecutor.ExecuteCommandAsync($"\"netsh AdvFirewall firewall add rule name=PSExec dir=In action=allow protocol=TCP localport=RPC profile=domain,private program=\"%WinDir%\\system32\\services.exe\" service=any remoteip={hostConfiguration.Server}\"");

        var localizedRuleGroupName = GetLocalizedRuleGroupName();
        await commandExecutor.ExecuteCommandAsync($"\"netsh AdvFirewall firewall set rule group=\"{localizedRuleGroupName}\" new enable=yes\"");
    }

    public async Task DisableAsync()
    {
        await commandExecutor.ExecuteCommandAsync("netsh AdvFirewall firewall delete rule name=PSExec");

        var localizedRuleGroupName = GetLocalizedRuleGroupName();
        await commandExecutor.ExecuteCommandAsync($"\"netsh AdvFirewall firewall set rule group=\"{localizedRuleGroupName}\" new enable=no\"");
    }

    private string GetLocalizedRuleGroupName()
    {
        var currentCulture = CultureInfo.CurrentCulture.Name;

        return _ruleGroupNames.TryGetValue(currentCulture, out var localizedGroupName) ? localizedGroupName : _ruleGroupNames["en-US"];
    }
}