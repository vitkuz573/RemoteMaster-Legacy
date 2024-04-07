using System.Diagnostics;
using Microsoft.Win32;
using RemoteMaster.Host.Windows.Abstractions;

namespace RemoteMaster.Host.Windows.Services;

public class WoLConfiguratorService : IWoLConfiguratorService
{
    private const string PowerSettingsKeyPath = @"SYSTEM\CurrentControlSet\Control\Session Manager\Power";
    private const string HiberbootEnabledValueName = "HiberbootEnabled";

    private const string NetworkAdaptersKeyPath = @"SYSTEM\CurrentControlSet\Control\Class\{4D36E972-E325-11CE-BFC1-08002BE10318}";
    private const string PnPCapabilitiesValueName = "PnPCapabilities";

    public void DisableFastStartup()
    {
        SetRegistryValue(PowerSettingsKeyPath, HiberbootEnabledValueName, 0);
    }

    public void DisablePnPEnergySaving()
    {
        using var adapters = Registry.LocalMachine.OpenSubKey(NetworkAdaptersKeyPath, true);

        if (adapters != null)
        {
            foreach (var subkeyName in adapters.GetSubKeyNames())
            {
                SetRegistryValue($"{NetworkAdaptersKeyPath}\\{subkeyName}", PnPCapabilitiesValueName, 0);
            }
        }
    }

    private static void SetRegistryValue(string keyPath, string valueName, int value)
    {
        using var key = Registry.LocalMachine.OpenSubKey(keyPath, true);

        if (key != null)
        {
            if ((int)key.GetValue(valueName, 1) != value)
            {
                key.SetValue(valueName, value, RegistryValueKind.DWord);
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
        
        using (var process = Process.Start(startInfoForDeviceQuery))
        {
            process.WaitForExit();
            programmableDevices = process.StandardOutput.ReadToEnd();
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

            using var powerCfgProcess = Process.Start(startInfoForEnableWake);
            powerCfgProcess.WaitForExit();
        }
    }
}
