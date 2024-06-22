// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using RemoteMaster.Shared.Enums;

namespace RemoteMaster.Shared.Dtos;

public class ConnectionRequest(Intention intention, string group, string userName, string role)
{
    public Intention Intention { get; } = intention;

    public string Group { get; } = group;

    public string UserName { get; } = userName;

    public string Role { get; } = role;
}
