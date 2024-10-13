// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using RemoteMaster.Server.Enums;

namespace RemoteMaster.Server.Models;

public class AuthenticationResult
{
    public AuthenticationStatus Status { get; set; }
    
    public string? ErrorMessage { get; set; }
    
    public string? RedirectUrl { get; set; }
}
