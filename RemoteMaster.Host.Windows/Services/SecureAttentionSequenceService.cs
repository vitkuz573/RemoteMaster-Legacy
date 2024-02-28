// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using Microsoft.Win32;
using RemoteMaster.Host.Windows.Abstractions;
using RemoteMaster.Host.Windows.Models;

namespace RemoteMaster.Host.Windows.Services;

public class SecureAttentionSequenceService : ISecureAttentionSequenceService
{
    private const string RegistryKeyPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System";
    private const string SasValueName = "SoftwareSASGeneration";

    public SoftwareSasOption SasOption
    {
        get
        {
            using var key = Registry.LocalMachine.OpenSubKey(RegistryKeyPath, false);

            var value = key?.GetValue(SasValueName);

            if (value is int intValue && Enum.IsDefined(typeof(SoftwareSasOption), intValue))
            {
                return (SoftwareSasOption)intValue;
            }

            return SoftwareSasOption.None;
        }
        set
        {
            using var key = Registry.LocalMachine.CreateSubKey(RegistryKeyPath, true) ?? throw new InvalidOperationException("Cannot access registry to change SAS setting.");
            key.SetValue(SasValueName, (int)value, RegistryValueKind.DWord);
        }
    }
}
