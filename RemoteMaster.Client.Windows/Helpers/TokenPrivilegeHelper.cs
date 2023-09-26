// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Diagnostics.CodeAnalysis;
using Windows.Win32.Security;
using static Windows.Win32.PInvoke;

namespace RemoteMaster.Client.Helpers;

public static class TokenPrivilegeHelper
{
    [SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "All disposable objects are properly handled.")]
    /// <summary>
    /// Adjusts the token privilege for the current process.
    /// </summary>
    /// <param name="privilegeName">The name of the privilege to adjust.</param>
    public static unsafe bool AdjustTokenPrivilege(string privilegeName)
    {
        if (string.IsNullOrEmpty(privilegeName))
        {
            throw new ArgumentNullException(nameof(privilegeName));
        }

        using var hProcess = GetCurrentProcess_SafeHandle();

        var tkp = new TOKEN_PRIVILEGES
        {
            PrivilegeCount = 1,
            Privileges =
            {
                _0 =
                {
                    Attributes = TOKEN_PRIVILEGES_ATTRIBUTES.SE_PRIVILEGE_ENABLED
                }
            }
        };

        if (!OpenProcessToken(hProcess, TOKEN_ACCESS_MASK.TOKEN_ADJUST_PRIVILEGES | TOKEN_ACCESS_MASK.TOKEN_QUERY, out var hToken))
        {
            return false;
        }

        using (hToken)
        {
            if (!LookupPrivilegeValue(null, privilegeName, out tkp.Privileges._0.Luid))
            {
                return false;
            }

            if (!AdjustTokenPrivileges(hToken, false, tkp, 0, null, (uint*)0))
            {
                return false;
            }
        }

        return true;
    }
}
