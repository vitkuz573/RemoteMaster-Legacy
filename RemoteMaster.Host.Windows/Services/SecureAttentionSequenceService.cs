// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using RemoteMaster.Host.Windows.Abstractions;
using RemoteMaster.Host.Windows.Enums;

namespace RemoteMaster.Host.Windows.Services;

public class SecureAttentionSequenceService(IRegistryService registryService, ILogger<SecureAttentionSequenceService> logger) : ISecureAttentionSequenceService
{
    private const string RegistryKeyPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System";
    private const string SasValueName = "SoftwareSASGeneration";

    public SoftwareSasOption GetSasOption()
    {
        try
        {
            using var key = registryService.OpenSubKey(RegistryHive.LocalMachine, RegistryKeyPath, writable: false);
            var value = key?.GetValue(SasValueName, null);

            return value is int intValue && Enum.IsDefined(typeof(SoftwareSasOption), intValue)
                ? (SoftwareSasOption)intValue
                : SoftwareSasOption.None;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error reading SAS option from registry. Returning default value.");
            
            return SoftwareSasOption.None;
        }
    }

    public void SetSasOption(SoftwareSasOption option)
    {
        try
        {
            using var key = registryService.OpenSubKey(RegistryHive.LocalMachine, RegistryKeyPath, writable: true) ?? throw new InvalidOperationException("Failed to access registry for modifying SAS option.");

            key.SetValue(SasValueName, (int)option, RegistryValueKind.DWord);
            logger.LogInformation("Successfully set SAS option to {SasOption}", option);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to set SAS option in registry.");
            throw;
        }
    }
}
