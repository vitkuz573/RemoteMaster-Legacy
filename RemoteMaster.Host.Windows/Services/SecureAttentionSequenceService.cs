// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using Microsoft.Win32;
using RemoteMaster.Host.Windows.Abstractions;

namespace RemoteMaster.Host.Windows.Services;

public class SecureAttentionSequenceService : ISecureAttentionSequenceService
{
    private const string RegistryKeyPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System";
    private const string SasValueName = "SoftwareSASGeneration";

    public bool IsEnabled
    {
        get
        {
            using var key = Registry.LocalMachine.OpenSubKey(RegistryKeyPath, false);
            
            if (key == null)
            {
                return false;
            }

            var value = key.GetValue(SasValueName);

            return value != null && value is int intValue && intValue == 3;
        }
    }

    public void Disable()
    {
        using var key = Registry.LocalMachine.CreateSubKey(RegistryKeyPath, true) ?? throw new InvalidOperationException("Cannot access registry to disable SAS.");
        key.SetValue(SasValueName, 0, RegistryValueKind.DWord);
    }

    public void Enable()
    {
        using var key = Registry.LocalMachine.CreateSubKey(RegistryKeyPath, true) ?? throw new InvalidOperationException("Cannot access registry to enable SAS.");
        key.SetValue(SasValueName, 3, RegistryValueKind.DWord);
    }
}
