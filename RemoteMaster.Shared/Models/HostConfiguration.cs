// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using RemoteMaster.Shared.DTOs;

namespace RemoteMaster.Shared.Models;

public class HostConfiguration(string server, SubjectDto subject, HostDto host)
{
    public string Server { get; } = server;

    public SubjectDto Subject { get; set; } = subject;

    public HostDto Host { get; set; } = host;
}
