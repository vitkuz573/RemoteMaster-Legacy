// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Diagnostics;
using System.Globalization;
using Microsoft.AspNetCore.SignalR;
using RemoteMaster.Host.Core.Abstractions;
using RemoteMaster.Host.Windows.Abstractions;
using RemoteMaster.Host.Windows.Hubs;
using RemoteMaster.Shared.Models;
using Serilog;
using static RemoteMaster.Shared.Models.Message;

namespace RemoteMaster.Host.Windows.Services;

public class PsExecService(IHostConfigurationService hostConfigurationService, IHubContext<ServiceHub, IServiceClient> hubContext) : IPsExecService
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
    }

    public async Task DisableAsync()
    {
        await ExecuteCommandAsync("netsh AdvFirewall firewall delete rule name=PSExec");

        var localizedRuleGroupName = GetLocalizedRuleGroupName();
        await ExecuteCommandAsync($"\"netsh AdvFirewall firewall set rule group=\"{localizedRuleGroupName}\" new enable=no\"");
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

            await hubContext.Clients.All.ReceiveMessage(new Message(process.Id.ToString(), MessageType.Service)
            {
                Meta = "pid"
            });

            var readErrorTask = ReadStreamAsync(process.StandardError, MessageType.Error);
            var readOutputTask = ReadStreamAsync(process.StandardOutput, MessageType.Information);

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

    private async Task ReadStreamAsync(TextReader streamReader, MessageType messageType)
    {
        while (await streamReader.ReadLineAsync() is { } line)
        {
            await hubContext.Clients.All.ReceiveMessage(new Message(line, messageType));
        }
    }
}
