// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

namespace RemoteMaster.Server.Models;

public class ConnectionDiagnostics
{
    public string ConnectionState { get; set; }

    public TimeSpan ConnectionDuration { get; set; }

    public string ConnectionId { get; set; }

    public string ProtocolUsed { get; set; }

    public long ReceivedMessagesCount { get; set; }
}
