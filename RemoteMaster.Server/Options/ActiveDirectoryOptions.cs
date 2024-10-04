// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

namespace RemoteMaster.Server.Options;

public class ActiveDirectoryOptions
{
    public string SearchBase { get; set; }

    public int KeySize { get; set; } = 2048;

    public int ValidityPeriod { get; set; } = 1;

    public string ActiveDirectoryServer { get; set; }

    public string TemplateName { get; set; }

    public string Username { get; set; }

    public string Password { get; set; }
}
