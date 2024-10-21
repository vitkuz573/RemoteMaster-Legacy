// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Security;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using RemoteMaster.Host.Windows.Abstractions;

namespace RemoteMaster.Host.Windows.Hubs;

[Authorize]
public class RegistryHub(IRegistryService registryService, ILogger<RegistryHub> logger) : Hub<IRegistryClient>
{
    public async Task GetRootKeys()
    {
        var rootKeys = registryService.GetRootKeys()
                                      .Select(key => key.Name)
                                      .ToList();

        await Clients.Caller.ReceiveRootKeys(rootKeys);
    }

    public async Task GetRegistryValue(RegistryHive hive, string keyPath, string valueName, object defaultValue)
    {
        var value = registryService.GetValue(hive, keyPath, valueName, defaultValue);
        
        await Clients.Caller.ReceiveRegistryValue(value);
    }

    public async Task SetRegistryValue(RegistryHive hive, string keyPath, string valueName, object value, RegistryValueKind valueKind)
    {
        registryService.SetValue(hive, keyPath, valueName, value, valueKind);

        await Clients.Caller.ReceiveOperationResult("Value set successfully.");
    }

    public async Task GetSubKeyNames(RegistryHive hive, string? keyPath, string parentKey)
    {
        logger.LogInformation("Fetching subkeys for hive: {Hive}, keyPath: {KeyPath}", hive, keyPath ?? "<root>");

        using var baseKey = RegistryKey.OpenBaseKey(hive, RegistryView.Default);
        
        if (baseKey == null)
        {
            logger.LogError("Failed to open base key: {Hive}", hive);
            await Clients.Caller.ReceiveSubKeyNames([], parentKey);

            return;
        }

        using var key = string.IsNullOrEmpty(keyPath) ? baseKey : baseKey.OpenSubKey(keyPath);
        
        if (key == null)
        {
            logger.LogError("Failed to open key: {KeyPath}", keyPath ?? "<root>");
            await Clients.Caller.ReceiveSubKeyNames([], parentKey);

            return;
        }

        var subKeyNames = key.GetSubKeyNames().ToList();

        logger.LogInformation("Fetched {SubKeyCount} subkeys for keyPath: {KeyPath}", subKeyNames.Count, keyPath ?? "<root>");

        await Clients.Caller.ReceiveSubKeyNames(subKeyNames, parentKey);
    }

    public async Task GetAllRegistryValues(RegistryHive hive, string keyPath)
    {
        logger.LogInformation("Fetching all registry values for hive: {Hive}, keyPath: {KeyPath}", hive, keyPath);

        var values = registryService.GetAllValues(hive, keyPath);

        if (values.Any())
        {
            logger.LogInformation("Fetched {ValuesCount} registry values for keyPath: {KeyPath}", values.Count(), keyPath);
        }
        else
        {
            logger.LogWarning("No registry values found for keyPath: {KeyPath}", keyPath);
        }

        await Clients.Caller.ReceiveAllRegistryValues(values);
    }

    public async Task<byte[]> ExportRegistryBranch(RegistryHive hive, string? keyPath)
    {
        logger.LogInformation("Exporting registry branch for hive: {Hive}, keyPath: {KeyPath}", hive, keyPath ?? "<root>");

        using var baseKey = RegistryKey.OpenBaseKey(hive, RegistryView.Default);

        using var key = string.IsNullOrEmpty(keyPath) ? baseKey : baseKey.OpenSubKey(keyPath);

        if (key == null)
        {
            logger.LogError("Failed to open key for export: {KeyPath}", keyPath ?? "<root>");

            return [];
        }

        using var memoryStream = new MemoryStream();
        await using var writer = new StreamWriter(memoryStream);

        await writer.WriteLineAsync("Windows Registry Editor Version 5.00");
        await writer.WriteLineAsync();

        ExportKey(writer, key, key.Name);

        await writer.FlushAsync();

        return memoryStream.ToArray();
    }

    private void ExportKey(StreamWriter writer, RegistryKey key, string path)
    {
        try
        {
            writer.WriteLine($"[{path}]");

            foreach (var valueName in key.GetValueNames())
            {
                var value = key.GetValue(valueName);
                var valueKind = key.GetValueKind(valueName);

                switch (valueKind)
                {
                    case RegistryValueKind.String:
                        if (value is string stringValue)
                        {
                            writer.WriteLine($"\"{valueName}\"=\"{stringValue}\"");
                        }
                        break;
                    case RegistryValueKind.ExpandString:
                        if (value is string expandStringValue)
                        {
                            writer.WriteLine($"\"{valueName}\"=hex(2):{ToHexString(expandStringValue)}");
                        }
                        break;
                    case RegistryValueKind.DWord:
                        if (value is int dwordValue)
                        {
                            writer.WriteLine($"\"{valueName}\"=dword:{dwordValue:x8}");
                        }
                        break;
                    case RegistryValueKind.QWord:
                        if (value is long qwordValue)
                        {
                            writer.WriteLine($"\"{valueName}\"=qword:{qwordValue:x16}");
                        }
                        break;
                    case RegistryValueKind.Binary:
                        if (value is byte[] byteArrayValue)
                        {
                            var hex = BitConverter.ToString(byteArrayValue).Replace("-", ",");
                            writer.WriteLine($"\"{valueName}\"=hex:{hex}");
                        }
                        break;
                    case RegistryValueKind.MultiString:
                        if (value is string[] multiStringValue)
                        {
                            var multiStringHex = ToMultiStringHex(multiStringValue);
                            writer.WriteLine($"\"{valueName}\"=hex(7):{multiStringHex}");
                        }
                        break;
                    case RegistryValueKind.None:
                        // logger.LogWarning($"Registry value kind 'None' for valueName: {valueName}");
                        break;
                    case RegistryValueKind.Unknown:
                        // logger.LogWarning($"Registry value kind 'Unknown' for valueName: {valueName}");
                        break;
                    default:
                        logger.LogWarning("Unsupported registry value type: {ValueKind} for valueName: {ValueName}", valueKind, valueName);
                        break;
                }
            }

            writer.WriteLine();

            foreach (var subKeyName in key.GetSubKeyNames())
            {
                using var subKey = key.OpenSubKey(subKeyName);
                if (subKey != null)
                {
                    ExportKey(writer, subKey, $"{path}\\{subKeyName}");
                }
            }
        }
        catch (UnauthorizedAccessException ex)
        {
            logger.LogWarning("Access denied to registry key: {Path}. Exception: {Message}", path, ex.Message);
        }
        catch (SecurityException ex)
        {
            logger.LogWarning("Security exception for registry key: {Path}. Exception: {Message}", path, ex.Message);
        }
    }

    private static string ToHexString(string value)
    {
        var bytes = Encoding.Unicode.GetBytes(value);

        return BitConverter.ToString(bytes).Replace("-", ",");
    }

    private static string ToMultiStringHex(string[] values)
    {
        var joinedStrings = string.Join("\0", values) + "\0\0";
        var bytes = Encoding.Unicode.GetBytes(joinedStrings);

        return BitConverter.ToString(bytes).Replace("-", ",");
    }
}
