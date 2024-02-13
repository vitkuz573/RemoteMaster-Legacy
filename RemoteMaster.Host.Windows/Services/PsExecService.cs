// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using Serilog;
using System.Diagnostics;
using System.Globalization;
using RemoteMaster.Host.Core.Abstractions;

namespace RemoteMaster.Host.Windows.Services;

public class PsExecService(IHostConfigurationService hostConfigurationService) : IPsExecService
{
    private readonly Dictionary<string, string> _ruleGroupNames = new()
    {
        { "en-US", "Remote Service Management" },
        { "ru-RU", "Удаленное управление службой" }
    };

    public async Task EnableAsync()
    {
        var hostConfiguration = await hostConfigurationService.LoadConfigurationAsync(false);
        
        await ExecuteCommandAsync("winrm qc -force");
        await ExecuteCommandAsync($"""netsh AdvFirewall firewall add rule name=PSExec dir=In action=allow protocol=TCP localport=RPC profile=domain,private program="%WinDir%\\system32\\services.exe" service=any remoteip={hostConfiguration.Server}""");
        
        var localizedRuleGroupName = GetLocalizedRuleGroupName();
        await ExecuteCommandAsync($"""netsh AdvFirewall firewall set rule group="{localizedRuleGroupName}" new enable=yes""");
        
        Log.Information("PsExec and WinRM configurations have been enabled.");
    }

    public async Task DisableAsync()
    {
        await ExecuteCommandAsync($"netsh AdvFirewall firewall delete rule name=PSExec");
        
        var localizedRuleGroupName = GetLocalizedRuleGroupName();
        await ExecuteCommandAsync($"""netsh AdvFirewall firewall set rule group="{localizedRuleGroupName}" new enable=no""");
        
        Log.Information("PsExec and WinRM configurations have been disabled.");
    }

    private static async Task ExecuteCommandAsync(string command)
    {
        try
        {
            var processStartInfo = new ProcessStartInfo("cmd.exe", $"/c {command}")
            {
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = new Process();
            process.StartInfo = processStartInfo;
            process.Start();

            var result = await process.StandardOutput.ReadToEndAsync();

            await process.WaitForExitAsync();
            Log.Information($"Executed command: {command}\nResult: {result}");
        }
        catch (Exception ex)
        {
            Log.Error($"Error executing command: {command}. Exception: {ex.Message}");
        }
    }

    private string GetLocalizedRuleGroupName()
    {
        var currentCulture = CultureInfo.CurrentCulture.Name;

        return _ruleGroupNames.TryGetValue(currentCulture, out var localizedGroupName) ? localizedGroupName : _ruleGroupNames["en-US"];
    }
}