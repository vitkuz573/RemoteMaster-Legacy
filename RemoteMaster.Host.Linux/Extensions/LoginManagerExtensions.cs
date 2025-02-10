// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using RemoteMaster.Host.Linux.Abstractions;

namespace RemoteMaster.Host.Linux.Extensions;

public static class LoginManagerExtensions
{
    public static Task<bool> GetEnableWallMessagesAsync(this ILoginManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<bool>("EnableWallMessages");
    }

    public static Task<string> GetWallMessageAsync(this ILoginManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<string>("WallMessage");
    }

    public static Task<uint> GetNAutoVTsAsync(this ILoginManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<uint>("NAutoVTs");
    }

    public static Task<string[]> GetKillOnlyUsersAsync(this ILoginManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<string[]>("KillOnlyUsers");
    }

    public static Task<string[]> GetKillExcludeUsersAsync(this ILoginManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<string[]>("KillExcludeUsers");
    }

    public static Task<bool> GetKillUserProcessesAsync(this ILoginManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<bool>("KillUserProcesses");
    }

    public static Task<string> GetRebootParameterAsync(this ILoginManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<string>("RebootParameter");
    }

    public static Task<bool> GetRebootToFirmwareSetupAsync(this ILoginManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<bool>("RebootToFirmwareSetup");
    }

    public static Task<ulong> GetRebootToBootLoaderMenuAsync(this ILoginManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<ulong>("RebootToBootLoaderMenu");
    }

    public static Task<string> GetRebootToBootLoaderEntryAsync(this ILoginManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<string>("RebootToBootLoaderEntry");
    }

    public static Task<string[]> GetBootLoaderEntriesAsync(this ILoginManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<string[]>("BootLoaderEntries");
    }

    public static Task<bool> GetIdleHintAsync(this ILoginManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<bool>("IdleHint");
    }

    public static Task<ulong> GetIdleSinceHintAsync(this ILoginManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<ulong>("IdleSinceHint");
    }

    public static Task<ulong> GetIdleSinceHintMonotonicAsync(this ILoginManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<ulong>("IdleSinceHintMonotonic");
    }

    public static Task<string> GetBlockInhibitedAsync(this ILoginManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<string>("BlockInhibited");
    }

    public static Task<string> GetDelayInhibitedAsync(this ILoginManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<string>("DelayInhibited");
    }

    public static Task<ulong> GetInhibitDelayMaxUSecAsync(this ILoginManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<ulong>("InhibitDelayMaxUSec");
    }

    public static Task<ulong> GetUserStopDelayUSecAsync(this ILoginManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<ulong>("UserStopDelayUSec");
    }

    public static Task<string> GetHandlePowerKeyAsync(this ILoginManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<string>("HandlePowerKey");
    }

    public static Task<string> GetHandlePowerKeyLongPressAsync(this ILoginManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<string>("HandlePowerKeyLongPress");
    }

    public static Task<string> GetHandleRebootKeyAsync(this ILoginManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<string>("HandleRebootKey");
    }

    public static Task<string> GetHandleRebootKeyLongPressAsync(this ILoginManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<string>("HandleRebootKeyLongPress");
    }

    public static Task<string> GetHandleSuspendKeyAsync(this ILoginManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<string>("HandleSuspendKey");
    }

    public static Task<string> GetHandleSuspendKeyLongPressAsync(this ILoginManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<string>("HandleSuspendKeyLongPress");
    }

    public static Task<string> GetHandleHibernateKeyAsync(this ILoginManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<string>("HandleHibernateKey");
    }

    public static Task<string> GetHandleHibernateKeyLongPressAsync(this ILoginManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<string>("HandleHibernateKeyLongPress");
    }

    public static Task<string> GetHandleLidSwitchAsync(this ILoginManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<string>("HandleLidSwitch");
    }

    public static Task<string> GetHandleLidSwitchExternalPowerAsync(this ILoginManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<string>("HandleLidSwitchExternalPower");
    }

    public static Task<string> GetHandleLidSwitchDockedAsync(this ILoginManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<string>("HandleLidSwitchDocked");
    }

    public static Task SetEnableWallMessagesAsync(this ILoginManager o, bool val)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.SetAsync("EnableWallMessages", val);
    }

    public static Task SetWallMessageAsync(this ILoginManager o, string val)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.SetAsync("WallMessage", val);
    }
}
