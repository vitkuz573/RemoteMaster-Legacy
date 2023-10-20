// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Diagnostics.CodeAnalysis;
using System.Runtime.Versioning;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.System.StationsAndDesktops;
using static Windows.Win32.PInvoke;

namespace RemoteMaster.Host.Helpers;

[SupportedOSPlatform("windows6.0.6000")]
public static class DesktopHelper
{
    private static CloseDesktopSafeHandle _lastInputDesktop;

    internal static CloseDesktopSafeHandle OpenInputDesktop() => OpenInputDesktop_SafeHandle(0, true, (DESKTOP_ACCESS_FLAGS)GENERIC_ACCESS_RIGHTS.GENERIC_ALL);

    public static unsafe bool GetCurrentDesktop([NotNullWhen(true)] out string? desktopName)
    {
        using var inputDesktop = OpenInputDesktop();

        const int maxLength = 256;

        fixed (char* pDesktopBytes = new char[maxLength])
        {
            uint cbLengthNeeded;

            if (!GetUserObjectInformation(inputDesktop, USER_OBJECT_INFORMATION_INDEX.UOI_NAME, pDesktopBytes, maxLength, &cbLengthNeeded))
            {
                desktopName = null;

                return false;
            }

            var charLength = (int)cbLengthNeeded / sizeof(char) - 1;
            desktopName = new string(pDesktopBytes, 0, charLength);

            return true;
        }
    }

    public static bool SwitchToInputDesktop()
    {
        try
        {
            _lastInputDesktop?.Close();

            using var inputDesktop = OpenInputDesktop();

            if (inputDesktop == null)
            {
                return false;
            }

            var result = SetThreadDesktop(inputDesktop) && SwitchDesktop(inputDesktop);
            _lastInputDesktop = inputDesktop;

            return result;
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error: " + ex.Message);

            return false;
        }
    }
}