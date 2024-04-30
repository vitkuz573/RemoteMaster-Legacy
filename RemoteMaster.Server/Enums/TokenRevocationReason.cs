// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

namespace RemoteMaster.Server.Enums;

public enum TokenRevocationReason
{
    None = 0,
    UserLoggedOut = 1,
    ReplacedDuringRefresh = 2,
    AdminRevoked = 3,
    SuspiciousActivity = 4
}
