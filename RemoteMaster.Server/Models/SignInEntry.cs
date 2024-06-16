// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using RemoteMaster.Server.Data;

namespace RemoteMaster.Server.Models;

public class SignInEntry
{
    public int Id { get; set; }

    public string UserId { get; set; }

    public DateTime SignInTime { get; set; }

    public bool IsSuccessful { get; set; }

    public string IpAddress { get; set; }

    public ApplicationUser User { get; set; }
}
