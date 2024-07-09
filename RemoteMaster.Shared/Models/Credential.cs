// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

namespace RemoteMaster.Shared.Models;

public class Credential(string username, string password)
{
    public string UserName { get; } = username;

    public string Password { get; } = password;
}
