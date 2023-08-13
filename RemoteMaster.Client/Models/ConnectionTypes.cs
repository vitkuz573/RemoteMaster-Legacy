// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using RemoteMaster.Client.Abstractions;

namespace RemoteMaster.Client.Models;

public static class ConnectionTypes
{
    public static IConnectionType Server { get; } = new ServerConnectionType();
    
    public static IConnectionType Agent { get; } = new AgentConnectionType();

    private class ServerConnectionType : IConnectionType
    {
        public string Name => "Server";
    }

    private class AgentConnectionType : IConnectionType
    {
        public string Name => "Agent";
    }
}