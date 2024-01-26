// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using RemoteMaster.Host.Windows.Abstractions;
using Windows.Win32.Security;
using static Windows.Win32.PInvoke;

namespace RemoteMaster.Host.Windows.Services;

public class TokenPrivilegeService : ITokenPrivilegeService
{
    public unsafe bool AdjustPrivilege(string privilegeName)
    {
        ArgumentException.ThrowIfNullOrEmpty(privilegeName);

        using var hProcess = GetCurrentProcess_SafeHandle();

        var tkp = new TOKEN_PRIVILEGES
        {
            PrivilegeCount = 1,
            Privileges =
            {
                e0 =
                {
                    Attributes = TOKEN_PRIVILEGES_ATTRIBUTES.SE_PRIVILEGE_ENABLED
                }
            }
        };

        try
        {
            if (!OpenProcessToken(hProcess, TOKEN_ACCESS_MASK.TOKEN_ADJUST_PRIVILEGES | TOKEN_ACCESS_MASK.TOKEN_QUERY, out var hToken))
            {
                return false;
            }

            using (hToken)
            {
                if (!LookupPrivilegeValue(null, privilegeName, out tkp.Privileges.e0.Luid))
                {
                    return false;
                }

                return AdjustTokenPrivileges(hToken, false, &tkp, 0, null, null);
            }
        }
        catch
        {
            return false;
        }
    }
}
