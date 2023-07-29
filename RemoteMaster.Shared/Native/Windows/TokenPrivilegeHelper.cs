using System.Runtime.Versioning;
using Windows.Win32.Security;
using static Windows.Win32.PInvoke;

namespace RemoteMaster.Shared.Native.Windows;

[SupportedOSPlatform("windows6.0.6000")]
public static class TokenPrivilegeHelper
{
    public static unsafe void AdjustTokenPrivilege(string privilegeName)
    {
        using var hProcess = GetCurrentProcess_SafeHandle();

        var tkp = new TOKEN_PRIVILEGES
        {
            PrivilegeCount = 1,
            Privileges =
            {
                _0 =
                {
                    Attributes = TOKEN_PRIVILEGES_ATTRIBUTES.SE_PRIVILEGE_ENABLED,
                }
            }
        };

        OpenProcessToken(hProcess, TOKEN_ACCESS_MASK.TOKEN_ADJUST_PRIVILEGES | TOKEN_ACCESS_MASK.TOKEN_QUERY, out var hToken);
        LookupPrivilegeValue(null, privilegeName, out tkp.Privileges._0.Luid);
        AdjustTokenPrivileges(hToken, false, tkp, 0, null, (uint*)0);
    }
}