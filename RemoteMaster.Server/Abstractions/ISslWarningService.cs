// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Net;

namespace RemoteMaster.Server.Abstractions;

public interface ISslWarningService
{
    bool IsSslAllowed(IPAddress ipAddress);

    void SetSslAllowance(IPAddress ipAddress, bool isAllowed);
}
