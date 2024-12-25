// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using Microsoft.AspNetCore.SignalR.Client;
using MudBlazor;
using RemoteMaster.Server.Models;
using RemoteMaster.Shared.DTOs;
using RemoteMaster.Shared.Models;

namespace RemoteMaster.Server.Components.Dialogs;

public partial class PsExecRulesDialog
{
    private bool _selectedOption;
    private readonly Dictionary<HostDto, HostResults> _resultsPerHost = [];
    private readonly HashSet<HubConnection> _subscribedConnections = [];

    private async Task Ok()
    {
        foreach (var (host, connection) in Hosts)
        {
            if (connection != null && !_subscribedConnections.Contains(connection))
            {
                connection.On<Message>("ReceiveMessage", async message =>
                {
                    UpdateResultsForHost(host, message);
                    await InvokeAsync(StateHasChanged);
                });

                _subscribedConnections.Add(connection);
            }

            if (connection != null)
            {
                await connection.InvokeAsync("SetPsExecRules", _selectedOption);
            }
        }
    }

    private void UpdateResultsForHost(HostDto host, Message scriptResult)
    {
        if (!_resultsPerHost.TryGetValue(host, out var results))
        {
            results = new HostResults();
            _resultsPerHost[host] = results;
        }

        if (scriptResult.Meta == MessageMeta.ProcessIdInformation)
        {
            results.LastPid = int.Parse(scriptResult.Text);
        }
        else
        {
            results.Messages.Add(scriptResult);
        }
    }

    private static Color GetColorBySeverity(Message.MessageSeverity severity)
    {
        return severity switch
        {
            Message.MessageSeverity.Error => Color.Error,
            Message.MessageSeverity.Warning => Color.Warning,
            Message.MessageSeverity.Information => Color.Default,
            _ => Color.Default
        };
    }
}
