// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using RemoteMaster.Host.Linux.Abstractions;
using RemoteMaster.Host.Linux.Models;
using Tmds.DBus;

namespace RemoteMaster.Host.Linux.Extensions;

public static class LoginSessionExtensions
{
    public static Task<string> GetIdAsync(this ILoginSession o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<string>(nameof(LoginSessionProperties.Id));
    }

    public static Task<(uint, ObjectPath)> GetUserAsync(this ILoginSession o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<(uint, ObjectPath)>(nameof(LoginSessionProperties.User));
    }

    public static Task<string> GetNameAsync(this ILoginSession o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<string>(nameof(LoginSessionProperties.Name));
    }

    public static Task<ulong> GetTimestampAsync(this ILoginSession o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<ulong>(nameof(LoginSessionProperties.Timestamp));
    }

    public static Task<ulong> GetTimestampMonotonicAsync(this ILoginSession o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<ulong>(nameof(LoginSessionProperties.TimestampMonotonic));
    }

    public static Task<uint> GetVTNrAsync(this ILoginSession o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<uint>(nameof(LoginSessionProperties.VTNr));
    }

    public static Task<(string, ObjectPath)> GetSeatAsync(this ILoginSession o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<(string, ObjectPath)>(nameof(LoginSessionProperties.Seat));
    }

    public static Task<string> GetTTYAsync(this ILoginSession o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<string>(nameof(LoginSessionProperties.TTY));
    }

    public static Task<string> GetDisplayAsync(this ILoginSession o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<string>(nameof(LoginSessionProperties.Display));
    }

    public static Task<bool> GetRemoteAsync(this ILoginSession o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<bool>(nameof(LoginSessionProperties.Remote));
    }

    public static Task<string> GetRemoteHostAsync(this ILoginSession o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<string>(nameof(LoginSessionProperties.RemoteHost));
    }

    public static Task<string> GetRemoteUserAsync(this ILoginSession o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<string>(nameof(LoginSessionProperties.RemoteUser));
    }

    public static Task<string> GetServiceAsync(this ILoginSession o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<string>(nameof(LoginSessionProperties.Service));
    }

    public static Task<string> GetDesktopAsync(this ILoginSession o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<string>(nameof(LoginSessionProperties.Desktop));
    }

    public static Task<string> GetScopeAsync(this ILoginSession o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<string>(nameof(LoginSessionProperties.Scope));
    }

    public static Task<uint> GetLeaderAsync(this ILoginSession o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<uint>(nameof(LoginSessionProperties.Leader));
    }

    public static Task<uint> GetAuditAsync(this ILoginSession o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<uint>(nameof(LoginSessionProperties.Audit));
    }

    public static Task<string> GetTypeAsync(this ILoginSession o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<string>(nameof(LoginSessionProperties.Type));
    }

    public static Task<string> GetClassAsync(this ILoginSession o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<string>(nameof(LoginSessionProperties.Class));
    }

    public static Task<bool> GetActiveAsync(this ILoginSession o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<bool>(nameof(LoginSessionProperties.Active));
    }

    public static Task<string> GetStateAsync(this ILoginSession o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<string>(nameof(LoginSessionProperties.State));
    }

    public static Task<bool> GetIdleHintAsync(this ILoginSession o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<bool>(nameof(LoginSessionProperties.IdleHint));
    }

    public static Task<ulong> GetIdleSinceHintAsync(this ILoginSession o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<ulong>(nameof(LoginSessionProperties.IdleSinceHint));
    }

    public static Task<ulong> GetIdleSinceHintMonotonicAsync(this ILoginSession o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<ulong>(nameof(LoginSessionProperties.IdleSinceHintMonotonic));
    }

    public static Task<bool> GetLockedHintAsync(this ILoginSession o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<bool>(nameof(LoginSessionProperties.LockedHint));
    }
}
