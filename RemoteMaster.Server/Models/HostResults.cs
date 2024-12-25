// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Text;
using RemoteMaster.Shared.Models;

namespace RemoteMaster.Server.Models;

public class HostResults
{
    public List<Message> Messages { get; } = [];

    public int? LastPid { get; set; }

    public override string ToString()
    {
        var sb = new StringBuilder();

        foreach (var message in Messages)
        {
            sb.AppendLine(message.Text);
        }

        return sb.ToString();
    }
}
