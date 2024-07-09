// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using RemoteMaster.Shared.Models;

namespace RemoteMaster.Shared.Dtos;

public class DomainUnjoinRequest(Credential userCredential)
{
    public Credential UserCredential { get; } = userCredential;

    public bool RemoveUserProfiles { get; init; }
}