// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Runtime.InteropServices;
using RemoteMaster.Host.Core.Abstractions;
using Windows.Win32.Foundation;
using Windows.Win32.System.SystemInformation;
using static Windows.Wdk.PInvoke;

namespace RemoteMaster.Host.Windows.Services;

public class OperatingSystemInformationService : IOperatingSystemInformationService
{
    public string GetName()
    {
        var versionInfo = new OSVERSIONINFOW
        {
            dwOSVersionInfoSize = (uint)Marshal.SizeOf(typeof(OSVERSIONINFOW))
        };

        var status = RtlGetVersion(ref versionInfo);

        if (status.SeverityCode != NTSTATUS.Severity.Success)
        {
            throw new InvalidOperationException($"RtlGetVersion failed with status code: {status}");
        }

        var isWindows11 = versionInfo is { dwMajorVersion: 10, dwBuildNumber: >= 22000 };
        var versionName = isWindows11 ? "Windows 11" : $"Windows {versionInfo.dwMajorVersion}.{versionInfo.dwMinorVersion}";

        return $"{versionName} (Build {versionInfo.dwBuildNumber})";
    }
}
