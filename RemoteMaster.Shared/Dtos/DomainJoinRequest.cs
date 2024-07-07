// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Net;

namespace RemoteMaster.Shared.Dtos;

public class DomainJoinRequest(string domain, NetworkCredential userCredentials)
{
    public string Domain { get; } = domain;

    public NetworkCredential UserCredentials { get; } = userCredentials;
}

