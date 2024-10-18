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
        var osVersionInfo = new OSVERSIONINFOW
        {
            dwOSVersionInfoSize = (uint)Marshal.SizeOf<OSVERSIONINFOW>()
        };

        var status = RtlGetVersion(ref osVersionInfo);

        if (status.SeverityCode != NTSTATUS.Severity.Success)
        {
            throw new InvalidOperationException($"RtlGetVersion failed with status: {status}");
        }

        var versionName = osVersionInfo switch
        {
            { dwMajorVersion: 10, dwBuildNumber: >= 22000 } => "Windows 11",
            { dwMajorVersion: 10 } => "Windows 10",
            _ => $"Windows {osVersionInfo.dwMajorVersion}.{osVersionInfo.dwMinorVersion}"
        };

        return $"{versionName} (Build {osVersionInfo.dwBuildNumber})";
    }
}
