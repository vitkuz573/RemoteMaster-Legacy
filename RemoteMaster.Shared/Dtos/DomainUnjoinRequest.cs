// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Net;

namespace RemoteMaster.Shared.Dtos;

public class DomainUnjoinRequest(NetworkCredential userCredentials)
{
    public NetworkCredential UserCredentials { get; } = userCredentials;

    public bool RemoveUserProfiles { get; init; }
}