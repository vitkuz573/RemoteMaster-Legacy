// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using RemoteMaster.Host.Windows.Abstractions;
using Serilog;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.System.StationsAndDesktops;
using static Windows.Win32.PInvoke;

namespace RemoteMaster.Host.Windows.Services;

public class DesktopService : IDesktopService
{
    private CloseDesktopSafeHandle? _lastInputDesktop;

    private static CloseDesktopSafeHandle OpenInputDesktop() => OpenInputDesktop_SafeHandle(0, true, (DESKTOP_ACCESS_FLAGS)GENERIC_ACCESS_RIGHTS.GENERIC_ALL);

    public bool SwitchToInputDesktop()
    {
        try
        {
            _lastInputDesktop?.Close();

            using var inputDesktop = OpenInputDesktop();

            var result = SetThreadDesktop(inputDesktop) && SwitchDesktop(inputDesktop);
            _lastInputDesktop = inputDesktop;

            if (result)
            {
                Log.Information("Successfully switched to input desktop.");
            }
            else
            {
                Log.Warning("Failed to switch to input desktop.");
            }

            return result;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error encountered while attempting to switch to input desktop.");

            return false;
        }
    }
}
