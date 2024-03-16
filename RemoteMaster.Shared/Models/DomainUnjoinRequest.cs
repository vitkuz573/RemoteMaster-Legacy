// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

namespace RemoteMaster.Shared.Models;

public class DomainUnjoinRequest(Credentials userCredentials)
{
    public Credentials UserCredentials { get; } = userCredentials;

    public bool RemoveUserProfiles { get; init; }
}