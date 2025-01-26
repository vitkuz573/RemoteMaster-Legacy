// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.IO.Pipes;
using System.Text;
using RemoteMaster.Host.Core.Abstractions;
using RemoteMaster.Host.Core.Models;

namespace RemoteMaster.Host.Core.Services;

public class CommandSender : ICommandSender
{
    public async Task SendCommandAsync(string command)
    {
        using var client = new NamedPipeClientStream(".", PipeNames.CommandPipe, PipeDirection.Out, PipeOptions.Asynchronous);
        await client.ConnectAsync(5000);

        using var writer = new StreamWriter(client, Encoding.UTF8)
        {
            AutoFlush = true
        };

        await writer.WriteLineAsync(command);
    }
}
