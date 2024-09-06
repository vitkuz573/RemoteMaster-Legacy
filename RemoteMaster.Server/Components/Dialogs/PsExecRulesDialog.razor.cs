// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using Microsoft.AspNetCore.SignalR.Client;
using RemoteMaster.Server.Aggregates.OrganizationAggregate;
using RemoteMaster.Server.Models;
using RemoteMaster.Shared.Models;

namespace RemoteMaster.Server.Components.Dialogs;

public partial class PsExecRulesDialog
{
    private bool _selectedOption;
    private readonly Dictionary<Computer, ComputerResults> _resultsPerComputer = [];
    private readonly HashSet<HubConnection> _subscribedConnections = [];

    private async Task Ok()
    {
        foreach (var (computer, connection) in Hosts)
        {
            if (connection != null && !_subscribedConnections.Contains(connection))
            {
                connection.On<Message>("ReceiveMessage", async message =>
                {
                    UpdateResultsForComputer(computer, message);
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

    private void UpdateResultsForComputer(Computer computer, Message scriptResult)
    {
        if (!_resultsPerComputer.TryGetValue(computer, out var results))
        {
            results = new ComputerResults();
            _resultsPerComputer[computer] = results;
        }

        if (scriptResult.Meta == "pid")
        {
            results.LastPid = int.Parse(scriptResult.Text);
        }
        else
        {
            results.Messages.AppendLine(scriptResult.Text);
        }
    }
}
