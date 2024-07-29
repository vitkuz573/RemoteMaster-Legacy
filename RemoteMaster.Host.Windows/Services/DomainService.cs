// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.ComponentModel;
using System.Security.Principal;
using Microsoft.Win32;
using RemoteMaster.Host.Core.Abstractions;
using RemoteMaster.Shared.DTOs;
using Serilog;
using Windows.Win32.NetworkManagement.NetManagement;
using static Windows.Win32.PInvoke;

namespace RemoteMaster.Host.Windows.Services;

/// <summary>
/// Provides services for managing domain membership of the machine.
/// </summary>
public class DomainService(IPowerService powerService) : IDomainService
{
    /// <summary>
    /// Joins the machine to a domain.
    /// </summary>
    /// <param name="domainJoinRequest">The request to join a domain, containing the domain name and user credentials.</param>
    public void JoinToDomain(DomainJoinRequest domainJoinRequest)
    {
        ArgumentNullException.ThrowIfNull(domainJoinRequest);

        var result = NetJoinDomain(null, domainJoinRequest.Domain, null, domainJoinRequest.UserCredentials.UserName, domainJoinRequest.UserCredentials.Password, NET_JOIN_DOMAIN_JOIN_OPTIONS.NETSETUP_JOIN_DOMAIN | NET_JOIN_DOMAIN_JOIN_OPTIONS.NETSETUP_ACCT_CREATE);

        if (result != 0)
        {
            throw new Win32Exception((int)result, "Failed to join the domain.");
        }

        var powerActionRequest = new PowerActionRequest
        {
            Message = string.Empty,
            Timeout = 0,
            ForceAppsClosed = true
        };

        powerService.Reboot(powerActionRequest);
    }

    /// <summary>
    /// Unjoins the machine from a domain.
    /// </summary>
    /// <param name="domainUnjoinRequest">The request to unjoin a domain, containing the user credentials.</param>
    public void UnjoinFromDomain(DomainUnjoinRequest domainUnjoinRequest)
    {
        ArgumentNullException.ThrowIfNull(domainUnjoinRequest);

        var result = NetUnjoinDomain(null, domainUnjoinRequest.UserCredentials.UserName, domainUnjoinRequest.UserCredentials.Password, NETSETUP_ACCT_DELETE);

        if (result != 0)
        {
            throw new Win32Exception((int)result, "Failed to unjoin from the domain.");
        }

        if (domainUnjoinRequest.RemoveUserProfiles)
        {
            const string profileListPath = @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\ProfileList";

            using var profileListKey = Registry.LocalMachine.OpenSubKey(profileListPath, true);

            if (profileListKey != null)
            {
                foreach (var sidString in profileListKey.GetSubKeyNames())
                {
                    using var sidKey = profileListKey.OpenSubKey(sidString, true);

                    if (sidKey != null)
                    {
                        var sid = new SecurityIdentifier(sidString);
                        var isDomainSid = sid.IsAccountSid() && !sid.IsWellKnown(WellKnownSidType.LocalSystemSid) && !sid.IsWellKnown(WellKnownSidType.LocalServiceSid) && !sid.IsWellKnown(WellKnownSidType.NetworkServiceSid);

                        if (!isDomainSid)
                        {
                            continue;
                        }

                        var profileImagePathValue = sidKey.GetValue("ProfileImagePath");

                        if (profileImagePathValue is not string profileImagePath || string.IsNullOrEmpty(profileImagePath) || !Directory.Exists(profileImagePath))
                        {
                            continue;
                        }

                        try
                        {
                            Directory.Delete(profileImagePath, true);
                            Log.Information("Profile directory {ProfileImagePath} deleted successfully.", profileImagePath);

                            profileListKey.DeleteSubKeyTree(sidString);
                            Log.Information("Registry key for SID {SID} deleted successfully.", sidString);
                        }
                        catch (Exception ex)
                        {
                            Log.Error(ex, "Error deleting profile directory {ProfileImagePath}.", profileImagePathValue);
                        }
                    }
                    else
                    {
                        Log.Warning("Subkey {SidString} not found.", sidString);
                    }
                }
            }
            else
            {
                Log.Warning("Registry key {ProfileListPath} not found.", profileListPath);
            }
        }

        var powerActionRequest = new PowerActionRequest
        {
            Message = string.Empty,
            Timeout = 0,
            ForceAppsClosed = true
        };

        powerService.Reboot(powerActionRequest);
    }
}
