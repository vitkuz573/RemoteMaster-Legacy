// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Security;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using RemoteMaster.Host.Windows.Abstractions;
using RemoteMaster.Shared.DTOs;

namespace RemoteMaster.Host.Windows.Services;

public class RegistryService(IRegistryKeyFactory registryKeyFactory, ILogger<RegistryService> logger) : IRegistryService
{
    public IEnumerable<IRegistryKey> GetRootKeys()
    {
        var keys = new List<IRegistryKey?>
        {
            registryKeyFactory.Create(RegistryHive.LocalMachine, string.Empty, false),
            registryKeyFactory.Create(RegistryHive.CurrentUser, string.Empty, false),
            registryKeyFactory.Create(RegistryHive.ClassesRoot, string.Empty, false),
            registryKeyFactory.Create(RegistryHive.Users, string.Empty, false),
            registryKeyFactory.Create(RegistryHive.CurrentConfig, string.Empty, false),
            registryKeyFactory.Create(RegistryHive.PerformanceData, string.Empty, false)
        };

        return keys.Where(key => key != null)!;
    }

    public IRegistryKey? OpenSubKey(RegistryHive hive, string keyPath, bool writable)
    {
        return registryKeyFactory.Create(hive, keyPath, writable);
    }

    public void SetValue(RegistryHive hive, string keyPath, string valueName, object value, RegistryValueKind valueKind)
    {
        using var key = OpenSubKey(hive, keyPath, true) ?? throw new InvalidOperationException($"Cannot open key at path '{keyPath}' for writing.");
        key.SetValue(valueName, value, valueKind);
    }

    public object GetValue(RegistryHive hive, string keyPath, string valueName, object defaultValue)
    {
        using var key = OpenSubKey(hive, keyPath, false);

        return key?.GetValue(valueName, defaultValue) ?? defaultValue;
    }

    public IEnumerable<RegistryValueDto> GetAllValues(RegistryHive hive, string keyPath)
    {
        using var key = OpenSubKey(hive, keyPath, false);

        if (key == null)
        {
            return [];
        }

        var valueNames = key.GetValueNames();

        return valueNames.Select(valueName => new RegistryValueDto
        {
            Name = valueName,
            Value = key.GetValue(valueName, null),
            ValueType = key.GetValueKind(valueName)
        }).ToList();
    }

    public async Task<IEnumerable<string>> GetSubKeyNamesAsync(RegistryHive hive, string keyPath)
    {
        logger.LogInformation("Fetching subkeys for hive: {Hive}, keyPath: {KeyPath}", hive, keyPath ?? "<root>");

        using var baseKey = RegistryKey.OpenBaseKey(hive, RegistryView.Default);
        using var key = string.IsNullOrEmpty(keyPath) ? baseKey : baseKey.OpenSubKey(keyPath);

        if (key == null)
        {
            logger.LogError("Failed to open key: {KeyPath}", keyPath ?? "<root>");

            return [];
        }

        var subKeyNames = key.GetSubKeyNames();
        logger.LogInformation("Fetched {SubKeyCount} subkeys for keyPath: {KeyPath}", subKeyNames.Length, keyPath ?? "<root>");

        return await Task.FromResult(subKeyNames);
    }

    public async Task<byte[]> ExportRegistryBranchAsync(RegistryHive hive, string? keyPath)
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
        using var writer = new StreamWriter(memoryStream);

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
                    case RegistryValueKind.Unknown:
                    default:
                        logger.LogDebug("Unsupported or unknown registry value type: {ValueKind} for valueName: {ValueName}", valueKind, valueName);
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
        catch (Exception ex)
        {
            logger.LogError("Unexpected error while exporting registry key: {Path}. Exception: {Message}", path, ex.Message);
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
