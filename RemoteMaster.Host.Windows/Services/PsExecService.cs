// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Diagnostics;
using Serilog;
using System.Globalization;
using RemoteMaster.Host.Core.Abstractions;
using RemoteMaster.Host.Windows.Models;
using Microsoft.AspNetCore.SignalR;
using RemoteMaster.Host.Core.Hubs;
using RemoteMaster.Shared.Models;
using static RemoteMaster.Shared.Models.ScriptResult;

namespace RemoteMaster.Host.Windows.Services;

public class PsExecService(IHostConfigurationService hostConfigurationService, IHubContext<ControlHub, IControlClient> hubContext) : IPsExecService
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
        await ExecuteCommandAsync($"\"netsh AdvFirewall firewall add rule name=PSExec dir=In action=allow protocol=TCP localport=RPC profile=domain,private program=\"%WinDir%\\system32\\services.exe\" service=any remoteip={hostConfiguration.Server}\"");

        var localizedRuleGroupName = GetLocalizedRuleGroupName();
        await ExecuteCommandAsync($"\"netsh AdvFirewall firewall set rule group=\"{localizedRuleGroupName}\" new enable=yes\"");

        Log.Information("PsExec and WinRM configurations have been enabled.");
    }

    public async Task DisableAsync()
    {
        await ExecuteCommandAsync("netsh AdvFirewall firewall delete rule name=PSExec");

        var localizedRuleGroupName = GetLocalizedRuleGroupName();
        await ExecuteCommandAsync($"\"netsh AdvFirewall firewall set rule group=\"{localizedRuleGroupName}\" new enable=no\"");

        Log.Information("PsExec and WinRM configurations have been disabled.");
    }

    private async Task ExecuteCommandAsync(string command)
    {
        try
        {
            var processStartInfo = new ProcessStartInfo("cmd.exe", $"/c {command}")
            {
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            using var process = new Process();
            process.StartInfo = processStartInfo;
            process.Start();

            await hubContext.Clients.All.ReceiveScriptResult(new ScriptResult
            {
                Message = process.Id.ToString(),
                Type = MessageType.Service,
                Meta = "pid"
            });

            var readErrorTask = ReadStreamAsync(process.StandardError, hubContext, MessageType.Error);
            var readOutputTask = ReadStreamAsync(process.StandardOutput, hubContext, MessageType.Output);

            await process.WaitForExitAsync();

            await Task.WhenAll(readErrorTask, readOutputTask);
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

    private static async Task ReadStreamAsync(TextReader streamReader, IHubContext<ControlHub, IControlClient> hubContext, MessageType messageType)
    {
        while (await streamReader.ReadLineAsync() is { } line)
        {
            await hubContext.Clients.All.ReceiveScriptResult(new ScriptResult
            {
                Message = line,
                Type = messageType
            });
        }
    }
}
