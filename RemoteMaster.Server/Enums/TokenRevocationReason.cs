// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

namespace RemoteMaster.Server.Enums;

/// <summary>
/// Enum representing the reasons for token revocation in the RemoteMaster project.
/// </summary>
public enum TokenRevocationReason
{
    /// <summary>
    /// Token was revoked because the user logged out.
    /// </summary>
    UserLoggedOut = 1,

    /// <summary>
    /// Token was replaced with a new one during the refresh process.
    /// </summary>
    Replaced = 2,

    /// <summary>
    /// Token was revoked by an administrator.
    /// </summary>
    RevokedByAdmin = 3,

    /// <summary>
    /// Token was revoked due to suspicious activity detected.
    /// </summary>
    SuspiciousActivity = 4,

    /// <summary>
    /// Token was preemptively revoked as a security measure.
    /// </summary>
    PreemptiveRevocation = 5,

    /// <summary>
    /// Token was revoked because the user's role has changed.
    /// </summary>
    RoleChanged = 6
}
