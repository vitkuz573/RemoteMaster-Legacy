// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using RemoteMaster.Host.Linux.Abstractions;
using RemoteMaster.Host.Linux.Models;

namespace RemoteMaster.Host.Linux.Extensions;

public static class LoginManagerExtensions
{
    public static Task<bool> GetEnableWallMessagesAsync(this ILoginManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<bool>(nameof(LoginManagerProperties.EnableWallMessages));
    }

    public static Task<string> GetWallMessageAsync(this ILoginManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<string>(nameof(LoginManagerProperties.WallMessage));
    }

    public static Task<uint> GetNAutoVTsAsync(this ILoginManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<uint>(nameof(LoginManagerProperties.NAutoVTs));
    }

    public static Task<string[]> GetKillOnlyUsersAsync(this ILoginManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<string[]>(nameof(LoginManagerProperties.KillOnlyUsers));
    }

    public static Task<string[]> GetKillExcludeUsersAsync(this ILoginManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<string[]>(nameof(LoginManagerProperties.KillExcludeUsers));
    }

    public static Task<bool> GetKillUserProcessesAsync(this ILoginManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<bool>(nameof(LoginManagerProperties.KillUserProcesses));
    }

    public static Task<string> GetRebootParameterAsync(this ILoginManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<string>(nameof(LoginManagerProperties.RebootParameter));
    }

    public static Task<bool> GetRebootToFirmwareSetupAsync(this ILoginManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<bool>(nameof(LoginManagerProperties.RebootToFirmwareSetup));
    }

    public static Task<ulong> GetRebootToBootLoaderMenuAsync(this ILoginManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<ulong>(nameof(LoginManagerProperties.RebootToBootLoaderMenu));
    }

    public static Task<string> GetRebootToBootLoaderEntryAsync(this ILoginManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<string>(nameof(LoginManagerProperties.RebootToBootLoaderEntry));
    }

    public static Task<string[]> GetBootLoaderEntriesAsync(this ILoginManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<string[]>(nameof(LoginManagerProperties.BootLoaderEntries));
    }

    public static Task<bool> GetIdleHintAsync(this ILoginManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<bool>(nameof(LoginManagerProperties.IdleHint));
    }

    public static Task<ulong> GetIdleSinceHintAsync(this ILoginManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<ulong>(nameof(LoginManagerProperties.IdleSinceHint));
    }

    public static Task<ulong> GetIdleSinceHintMonotonicAsync(this ILoginManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<ulong>(nameof(LoginManagerProperties.IdleSinceHintMonotonic));
    }

    public static Task<string> GetBlockInhibitedAsync(this ILoginManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<string>(nameof(LoginManagerProperties.BlockInhibited));
    }

    public static Task<string> GetDelayInhibitedAsync(this ILoginManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<string>(nameof(LoginManagerProperties.DelayInhibited));
    }

    public static Task<ulong> GetInhibitDelayMaxUSecAsync(this ILoginManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<ulong>(nameof(LoginManagerProperties.InhibitDelayMaxUSec));
    }

    public static Task<ulong> GetUserStopDelayUSecAsync(this ILoginManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<ulong>(nameof(LoginManagerProperties.UserStopDelayUSec));
    }

    public static Task<string> GetHandlePowerKeyAsync(this ILoginManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<string>(nameof(LoginManagerProperties.HandlePowerKey));
    }

    public static Task<string> GetHandlePowerKeyLongPressAsync(this ILoginManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<string>(nameof(LoginManagerProperties.HandlePowerKeyLongPress));
    }

    public static Task<string> GetHandleRebootKeyAsync(this ILoginManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<string>(nameof(LoginManagerProperties.HandleRebootKey));
    }

    public static Task<string> GetHandleRebootKeyLongPressAsync(this ILoginManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<string>(nameof(LoginManagerProperties.HandleRebootKeyLongPress));
    }

    public static Task<string> GetHandleSuspendKeyAsync(this ILoginManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<string>(nameof(LoginManagerProperties.HandleSuspendKey));
    }

    public static Task<string> GetHandleSuspendKeyLongPressAsync(this ILoginManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<string>(nameof(LoginManagerProperties.HandleSuspendKeyLongPress));
    }

    public static Task<string> GetHandleHibernateKeyAsync(this ILoginManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<string>(nameof(LoginManagerProperties.HandleHibernateKey));
    }

    public static Task<string> GetHandleHibernateKeyLongPressAsync(this ILoginManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<string>(nameof(LoginManagerProperties.HandleHibernateKeyLongPress));
    }

    public static Task<string> GetHandleLidSwitchAsync(this ILoginManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<string>(nameof(LoginManagerProperties.HandleLidSwitch));
    }

    public static Task<string> GetHandleLidSwitchExternalPowerAsync(this ILoginManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<string>(nameof(LoginManagerProperties.HandleLidSwitchExternalPower));
    }

    public static Task<string> GetHandleLidSwitchDockedAsync(this ILoginManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<string>(nameof(LoginManagerProperties.HandleLidSwitchDocked));
    }

    public static Task SetEnableWallMessagesAsync(this ILoginManager o, bool val)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.SetAsync(nameof(LoginManagerProperties.EnableWallMessages), val);
    }

    public static Task SetWallMessageAsync(this ILoginManager o, string val)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.SetAsync(nameof(LoginManagerProperties.WallMessage), val);
    }
}
