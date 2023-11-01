// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.ComponentModel;
using System.DirectoryServices;
using System.Security.Principal;
using Microsoft.Win32;
using RemoteMaster.Host.Core.Abstractions;
using Serilog;
using Windows.Win32.NetworkManagement.NetManagement;
using Windows.Win32.Storage.FileSystem;
using static Windows.Win32.PInvoke;

namespace RemoteMaster.Host.Windows.Services;

public class DomainService : IDomainService
{
    public void JoinToDomain(string domain, string user, string password)
    {
        var result = NetJoinDomain(null, domain, null, user, password, NET_JOIN_DOMAIN_JOIN_OPTIONS.NETSETUP_JOIN_DOMAIN | NET_JOIN_DOMAIN_JOIN_OPTIONS.NETSETUP_ACCT_CREATE);

        if (result != 0)
        {
            throw new Win32Exception((int)result, "Failed to join the domain.");
        }
    }

    public void UnjoinFromDomain(string user, string password)
    {
        var domainSid = GetDomainSid();

        Log.Information("Domain SID: {Sid}", domainSid);

        var result = NetUnjoinDomain(null, user, password, NETSETUP_ACCT_DELETE);
        
        if (result != 0)
        {
            throw new Win32Exception((int)result, "Failed to unjoin from the domain.");
        }

        if (!string.IsNullOrEmpty(domainSid))
        {
            DeleteDomainProfiles(domainSid);
        }
    }


    private static string? GetDomainSid()
    {
        try
        {
            using var domainEntry = new DirectoryEntry("LDAP://rootDSE");
            domainEntry.RefreshCache(new[] { "defaultNamingContext" });
            var defaultNamingContext = domainEntry.Properties["defaultNamingContext"].Value.ToString();
            
            using var defaultEntry = new DirectoryEntry($"LDAP://{defaultNamingContext}");
            var sidBytes = (byte[])defaultEntry.Properties["objectSid"].Value;
            var sid = new SecurityIdentifier(sidBytes, 0);

            return sid.ToString();
        }
        catch
        {
            return null;
        }
    }

    private static void DeleteDomainProfiles(string domainSid)
    {
        const string profileListPath = @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\ProfileList";
        using var registryKey = Registry.LocalMachine.OpenSubKey(profileListPath, true);

        foreach (var subKeyName in registryKey.GetSubKeyNames())
        {
            if (subKeyName.StartsWith(domainSid))
            {
                using var subKey = registryKey.OpenSubKey(subKeyName, true);
                var profileImagePath = subKey.GetValue("ProfileImagePath")?.ToString();

                if (!string.IsNullOrEmpty(profileImagePath) && Directory.Exists(profileImagePath))
                {
                    var moveResult = MoveFileEx(profileImagePath, null, MOVE_FILE_FLAGS.MOVEFILE_DELAY_UNTIL_REBOOT);

                    if (!moveResult)
                    {
                        Log.Error("Failed to schedule deletion of user profile directory {ProfileImagePath}.", profileImagePath);
                    }
                }

                try
                {
                    registryKey.DeleteSubKey(subKeyName);
                }
                catch (Exception ex)
                {
                    Log.Error("Failed to delete registry key {SubKeyName}. Error: {Message}", subKeyName, ex.Message);
                }
            }
        }
    }
}
