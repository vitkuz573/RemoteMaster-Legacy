// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using Microsoft.Win32.SafeHandles;
using Windows.Win32.Security;
using static Windows.Win32.PInvoke;

namespace RemoteMaster.Host.Helpers;

public static class TokenPrivilegeHelper
{
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

        SafeFileHandle hToken = null;

        try
        {
            var tokenOpened = OpenProcessToken(hProcess, TOKEN_ACCESS_MASK.TOKEN_ADJUST_PRIVILEGES | TOKEN_ACCESS_MASK.TOKEN_QUERY, out hToken);
            hToken ??= new SafeFileHandle(nint.Zero, false);

            if (!tokenOpened)
            {
                return false;
            }

            if (!LookupPrivilegeValue(null, privilegeName, out tkp.Privileges._0.Luid))
            {
                return false;
            }

            return AdjustTokenPrivileges(hToken, false, tkp, 0, null, null);
        }
        finally
        {
            hToken?.Dispose();
        }
    }
}
