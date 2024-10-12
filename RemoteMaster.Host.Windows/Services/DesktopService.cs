// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using Microsoft.Extensions.Logging;
using RemoteMaster.Host.Windows.Abstractions;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.System.StationsAndDesktops;
using static Windows.Win32.PInvoke;

namespace RemoteMaster.Host.Windows.Services;

public class DesktopService(ILogger<DesktopService> logger) : IDesktopService
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
                logger.LogInformation("Successfully switched to input desktop.");
            }
            else
            {
                logger.LogWarning("Failed to switch to input desktop.");
            }

            return result;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error encountered while attempting to switch to input desktop.");

            return false;
        }
    }
}
