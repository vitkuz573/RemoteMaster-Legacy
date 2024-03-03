// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.
namespace RemoteMaster.Shared.Models;

public class DomainJoinRequest(string domain, Credentials userCredentials)
{
    public string Domain { get; } = domain;

    public Credentials UserCredentials { get; } = userCredentials;
}

