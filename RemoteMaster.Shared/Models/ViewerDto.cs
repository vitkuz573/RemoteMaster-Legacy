// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

namespace RemoteMaster.Shared.Models;

public class ViewerDto(string connectionId, string group, string userName, string role, DateTime connectedTime, string ipAddress, string authenticationType)
{
    public string ConnectionId { get; } = connectionId;

    public string Group { get; } = group;

    public string UserName { get; } = userName;

    public string Role { get; } = role;

    public DateTime ConnectedTime { get; } = connectedTime;

    public string IpAddress { get; } = ipAddress;

    public string AuthenticationType { get; } = authenticationType;
}
