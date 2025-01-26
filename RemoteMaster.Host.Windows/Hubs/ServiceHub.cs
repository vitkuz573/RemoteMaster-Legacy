// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using RemoteMaster.Host.Core.Abstractions;
using RemoteMaster.Host.Windows.Abstractions;
using RemoteMaster.Shared.Models;

namespace RemoteMaster.Host.Windows.Hubs;

public class ServiceHub(IPsExecService psExecService, ICommandSender commandSender, ILogger<ServiceHub> logger) : Hub<IServiceClient>
{
    public async override Task OnConnectedAsync()
    {
        var user = Context.User;

        if (user == null)
        {
            var message = new Message("User is not authenticated.", Message.MessageSeverity.Error)
            {
                Meta = MessageMeta.AuthorizationError
            };

            await Clients.Caller.ReceiveMessage(message);

            Context.Abort();

            return;
        }
    }

    [Authorize(Policy = "ExecuteScriptPolicy")]
    public async Task SetPsExecRules(bool enable)
    {
        psExecService.Disable();

        if (enable)
        {
            await psExecService.EnableAsync();
        }
    }

    public async Task SendCommandToService(string command)
    {
        logger.LogInformation("Received command: {Command}", command);

        await commandSender.SendCommandAsync(command);
    }
}
