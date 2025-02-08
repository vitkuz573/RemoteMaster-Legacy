// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using RemoteMaster.Host.Core.Abstractions;
using RemoteMaster.Host.Windows.Abstractions;

namespace RemoteMaster.Host.Windows.Services;

public class WoLConfiguratorService(IRegistryService registryService, IProcessService processService, IProcessWrapperFactory processWrapperFactory, ILogger<WoLConfiguratorService> logger) : IWoLConfiguratorService
{
    private const string PowerSettingsKeyPath = @"SYSTEM\CurrentControlSet\Control\Session Manager\Power";
    private const string HiberbootEnabledValueName = "HiberbootEnabled";

    private const string NetworkAdaptersKeyPath = @"SYSTEM\CurrentControlSet\Control\Class\{4D36E972-E325-11CE-BFC1-08002BE10318}";
    private const string PnPCapabilitiesValueName = "PnPCapabilities";

    public void DisableFastStartup()
    {
        registryService.SetValue(RegistryHive.LocalMachine, PowerSettingsKeyPath, HiberbootEnabledValueName, 0, RegistryValueKind.DWord);
    }

    public void DisablePnPEnergySaving()
    {
        using var adapters = registryService.OpenSubKey(RegistryHive.LocalMachine, NetworkAdaptersKeyPath, true);

        if (adapters == null)
        {
            return;
        }

        foreach (var subkeyName in adapters.GetSubKeyNames())
        {
            registryService.SetValue(RegistryHive.LocalMachine, $"{NetworkAdaptersKeyPath}\\{subkeyName}", PnPCapabilitiesValueName, 0, RegistryValueKind.DWord);
        }
    }

    public async Task EnableWakeOnLanForAllAdaptersAsync()
    {
        try
        {
            string programmableDevices;

            var process = processWrapperFactory.Create();

            process.Start(new ProcessStartInfo
            {
                FileName = "powercfg.exe",
                Arguments = "/devicequery wake_programmable",
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardOutput = true
            });

            process.WaitForExit();

            programmableDevices = await processService.ReadStandardOutputAsync(process);

            var deviceNames = programmableDevices.Split(["\r\n", "\r", "\n"], StringSplitOptions.RemoveEmptyEntries);

            foreach (var deviceName in deviceNames)
            {
                var powerCfgProcess = processWrapperFactory.Create();

                powerCfgProcess.Start(new ProcessStartInfo
                {
                    FileName = "powercfg.exe",
                    Arguments = $"/deviceenablewake \"{deviceName}\"",
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    RedirectStandardOutput = true
                });

                process.WaitForExit();
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while enabling Wake on LAN for all adapters.");
            throw;
        }
    }
}
