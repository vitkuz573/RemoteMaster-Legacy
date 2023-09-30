// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Net.NetworkInformation;

namespace RemoteMaster.Agent.Core.Extensions;

public static class NetworkInterfaceExtensions
{
    public static string GetMacAddress(this NetworkInterface networkInterface)
    {
        if (networkInterface == null)
        {
            throw new ArgumentNullException(nameof(networkInterface));
        }

        return BitConverter.ToString(networkInterface.GetPhysicalAddress().GetAddressBytes());
    }
}