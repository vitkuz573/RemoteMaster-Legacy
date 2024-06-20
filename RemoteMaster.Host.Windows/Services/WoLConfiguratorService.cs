// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Diagnostics;
using Microsoft.Win32;
using RemoteMaster.Host.Windows.Abstractions;

namespace RemoteMaster.Host.Windows.Services;

public class WoLConfiguratorService(IRegistryService registryService, IProcessService processService) : IWoLConfiguratorService
{
    private const string PowerSettingsKeyPath = @"SYSTEM\CurrentControlSet\Control\Session Manager\Power";
    private const string HiberbootEnabledValueName = "HiberbootEnabled";

    private const string NetworkAdaptersKeyPath = @"SYSTEM\CurrentControlSet\Control\Class\{4D36E972-E325-11CE-BFC1-08002BE10318}";
    private const string PnPCapabilitiesValueName = "PnPCapabilities";

    public void DisableFastStartup()
    {
        registryService.SetValue(PowerSettingsKeyPath, HiberbootEnabledValueName, 0, RegistryValueKind.DWord);
    }

    public void DisablePnPEnergySaving()
    {
        using var adapters = registryService.OpenSubKey(NetworkAdaptersKeyPath, true);

        if (adapters != null)
        {
            foreach (var subkeyName in adapters.GetSubKeyNames())
            {
                registryService.SetValue($"{NetworkAdaptersKeyPath}\\{subkeyName}", PnPCapabilitiesValueName, 0, RegistryValueKind.DWord);
            }
        }
    }

    public void EnableWakeOnLanForAllAdapters()
    {
        var startInfoForDeviceQuery = new ProcessStartInfo
        {
            FileName = "powercfg.exe",
            Arguments = "/devicequery wake_programmable",
            CreateNoWindow = true,
            UseShellExecute = false,
            RedirectStandardOutput = true
        };

        var programmableDevices = string.Empty;

        using (var process = processService.Start(startInfoForDeviceQuery))
        {
            processService.WaitForExit(process);
            programmableDevices = processService.ReadStandardOutput(process);
        }

        var deviceNames = programmableDevices.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);

        foreach (var deviceName in deviceNames)
        {
            var startInfoForEnableWake = new ProcessStartInfo
            {
                FileName = "powercfg.exe",
                Arguments = $"/deviceenablewake \"{deviceName}\"",
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardOutput = true
            };

            using var powerCfgProcess = processService.Start(startInfoForEnableWake);
            processService.WaitForExit(powerCfgProcess);
        }
    }
}
