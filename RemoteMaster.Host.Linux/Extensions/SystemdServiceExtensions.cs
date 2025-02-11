// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using RemoteMaster.Host.Linux.Abstractions;
using RemoteMaster.Host.Linux.Models;

namespace RemoteMaster.Host.Linux.Extensions;

public static class SystemdServiceExtensions
{
    public static Task<string> GetTypeAsync(this ISystemdService o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<string>(nameof(SystemdServiceProperties.Type));
    }

    public static Task<string> GetExitTypeAsync(this ISystemdService o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<string>(nameof(SystemdServiceProperties.ExitType));
    }

    public static Task<string> GetRestartAsync(this ISystemdService o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<string>(nameof(SystemdServiceProperties.Restart));
    }

    public static Task<string> GetRestartModeAsync(this ISystemdService o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<string>(nameof(SystemdServiceProperties.RestartMode));
    }

    public static Task<string> GetPIDFileAsync(this ISystemdService o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<string>(nameof(SystemdServiceProperties.PIDFile));
    }

    public static Task<string> GetNotifyAccessAsync(this ISystemdService o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<string>(nameof(SystemdServiceProperties.NotifyAccess));
    }

    public static Task<ulong> GetRestartUSecAsync(this ISystemdService o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<ulong>(nameof(SystemdServiceProperties.RestartUSec));
    }

    public static Task<uint> GetRestartStepsAsync(this ISystemdService o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<uint>(nameof(SystemdServiceProperties.RestartSteps));
    }

    public static Task<ulong> GetRestartMaxDelayUSecAsync(this ISystemdService o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<ulong>(nameof(SystemdServiceProperties.RestartMaxDelayUSec));
    }

    public static Task<ulong> GetRestartUSecNextAsync(this ISystemdService o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<ulong>(nameof(SystemdServiceProperties.RestartUSecNext));
    }

    public static Task<ulong> GetTimeoutStartUSecAsync(this ISystemdService o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<ulong>(nameof(SystemdServiceProperties.TimeoutStartUSec));
    }

    public static Task<ulong> GetTimeoutStopUSecAsync(this ISystemdService o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<ulong>(nameof(SystemdServiceProperties.TimeoutStopUSec));
    }

    public static Task<ulong> GetTimeoutAbortUSecAsync(this ISystemdService o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<ulong>(nameof(SystemdServiceProperties.TimeoutAbortUSec));
    }

    public static Task<string> GetTimeoutStartFailureModeAsync(this ISystemdService o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<string>(nameof(SystemdServiceProperties.TimeoutStartFailureMode));
    }

    public static Task<string> GetTimeoutStopFailureModeAsync(this ISystemdService o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<string>(nameof(SystemdServiceProperties.TimeoutStopFailureMode));
    }

    public static Task<ulong> GetRuntimeMaxUSecAsync(this ISystemdService o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<ulong>(nameof(SystemdServiceProperties.RuntimeMaxUSec));
    }

    public static Task<ulong> GetRuntimeRandomizedExtraUSecAsync(this ISystemdService o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<ulong>(nameof(SystemdServiceProperties.RuntimeRandomizedExtraUSec));
    }

    public static Task<ulong> GetWatchdogUSecAsync(this ISystemdService o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<ulong>(nameof(SystemdServiceProperties.WatchdogUSec));
    }

    public static Task<ulong> GetWatchdogTimestampAsync(this ISystemdService o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<ulong>(nameof(SystemdServiceProperties.WatchdogTimestamp));
    }

    public static Task<ulong> GetWatchdogTimestampMonotonicAsync(this ISystemdService o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<ulong>(nameof(SystemdServiceProperties.WatchdogTimestampMonotonic));
    }

    public static Task<bool> GetRootDirectoryStartOnlyAsync(this ISystemdService o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<bool>(nameof(SystemdServiceProperties.RootDirectoryStartOnly));
    }

    public static Task<bool> GetRemainAfterExitAsync(this ISystemdService o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<bool>(nameof(SystemdServiceProperties.RemainAfterExit));
    }

    public static Task<bool> GetGuessMainPIDAsync(this ISystemdService o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<bool>(nameof(SystemdServiceProperties.GuessMainPID));
    }

    public static Task<(int[], int[])> GetRestartPreventExitStatusAsync(this ISystemdService o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<(int[], int[])>(nameof(SystemdServiceProperties.RestartPreventExitStatus));
    }

    public static Task<(int[], int[])> GetRestartForceExitStatusAsync(this ISystemdService o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<(int[], int[])>(nameof(SystemdServiceProperties.RestartForceExitStatus));
    }

    public static Task<(int[], int[])> GetSuccessExitStatusAsync(this ISystemdService o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<(int[], int[])>(nameof(SystemdServiceProperties.SuccessExitStatus));
    }

    public static Task<uint> GetMainPIDAsync(this ISystemdService o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<uint>(nameof(SystemdServiceProperties.MainPID));
    }

    public static Task<uint> GetControlPIDAsync(this ISystemdService o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<uint>(nameof(SystemdServiceProperties.ControlPID));
    }

    public static Task<string> GetBusNameAsync(this ISystemdService o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<string>(nameof(SystemdServiceProperties.BusName));
    }

    public static Task<uint> GetFileDescriptorStoreMaxAsync(this ISystemdService o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<uint>(nameof(SystemdServiceProperties.FileDescriptorStoreMax));
    }

    public static Task<uint> GetNFileDescriptorStoreAsync(this ISystemdService o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<uint>(nameof(SystemdServiceProperties.NFileDescriptorStore));
    }

    public static Task<string> GetFileDescriptorStorePreserveAsync(this ISystemdService o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<string>(nameof(SystemdServiceProperties.FileDescriptorStorePreserve));
    }

    public static Task<string> GetStatusTextAsync(this ISystemdService o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<string>(nameof(SystemdServiceProperties.StatusText));
    }

    public static Task<int> GetStatusErrnoAsync(this ISystemdService o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<int>(nameof(SystemdServiceProperties.StatusErrno));
    }

    public static Task<string> GetResultAsync(this ISystemdService o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<string>(nameof(SystemdServiceProperties.Result));
    }

    public static Task<string> GetReloadResultAsync(this ISystemdService o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<string>(nameof(SystemdServiceProperties.ReloadResult));
    }

    public static Task<string> GetCleanResultAsync(this ISystemdService o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<string>(nameof(SystemdServiceProperties.CleanResult));
    }

    public static Task<string> GetUSBFunctionDescriptorsAsync(this ISystemdService o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<string>(nameof(SystemdServiceProperties.USBFunctionDescriptors));
    }

    public static Task<string> GetUSBFunctionStringsAsync(this ISystemdService o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<string>(nameof(SystemdServiceProperties.USBFunctionStrings));
    }

    public static Task<uint> GetUIDAsync(this ISystemdService o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<uint>(nameof(SystemdServiceProperties.UID));
    }

    public static Task<uint> GetGIDAsync(this ISystemdService o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<uint>(nameof(SystemdServiceProperties.GID));
    }

    public static Task<uint> GetNRestartsAsync(this ISystemdService o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<uint>(nameof(SystemdServiceProperties.NRestarts));
    }

    public static Task<string> GetOOMPolicyAsync(this ISystemdService o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<string>(nameof(SystemdServiceProperties.OOMPolicy));
    }

    public static Task<(string, string, ulong)[]> GetOpenFileAsync(this ISystemdService o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<(string, string, ulong)[]>(nameof(SystemdServiceProperties.OpenFile));
    }

    public static Task<int> GetReloadSignalAsync(this ISystemdService o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<int>(nameof(SystemdServiceProperties.ReloadSignal));
    }

    public static Task<ulong> GetExecMainStartTimestampAsync(this ISystemdService o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<ulong>(nameof(SystemdServiceProperties.ExecMainStartTimestamp));
    }

    public static Task<ulong> GetExecMainStartTimestampMonotonicAsync(this ISystemdService o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<ulong>(nameof(SystemdServiceProperties.ExecMainStartTimestampMonotonic));
    }

    public static Task<ulong> GetExecMainExitTimestampAsync(this ISystemdService o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<ulong>(nameof(SystemdServiceProperties.ExecMainExitTimestamp));
    }

    public static Task<ulong> GetExecMainExitTimestampMonotonicAsync(this ISystemdService o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<ulong>(nameof(SystemdServiceProperties.ExecMainExitTimestampMonotonic));
    }

    public static Task<uint> GetExecMainPIDAsync(this ISystemdService o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<uint>(nameof(SystemdServiceProperties.ExecMainPID));
    }

    public static Task<int> GetExecMainCodeAsync(this ISystemdService o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<int>(nameof(SystemdServiceProperties.ExecMainCode));
    }

    public static Task<int> GetExecMainStatusAsync(this ISystemdService o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<int>(nameof(SystemdServiceProperties.ExecMainStatus));
    }

    public static Task<(string, string[], bool, ulong, ulong, ulong, ulong, uint, int, int)[]> GetExecConditionAsync(this ISystemdService o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<(string, string[], bool, ulong, ulong, ulong, ulong, uint, int, int)[]>(nameof(SystemdServiceProperties.ExecCondition));
    }

    public static Task<(string, string[], string[], ulong, ulong, ulong, ulong, uint, int, int)[]> GetExecConditionExAsync(this ISystemdService o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<(string, string[], string[], ulong, ulong, ulong, ulong, uint, int, int)[]>(
            "ExecConditionEx");
    }

    public static Task<(string, string[], bool, ulong, ulong, ulong, ulong, uint, int, int)[]> GetExecStartPreAsync(this ISystemdService o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<(string, string[], bool, ulong, ulong, ulong, ulong, uint, int, int)[]>(nameof(SystemdServiceProperties.ExecStartPre));
    }

    public static Task<(string, string[], string[], ulong, ulong, ulong, ulong, uint, int, int)[]> GetExecStartPreExAsync(this ISystemdService o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<(string, string[], string[], ulong, ulong, ulong, ulong, uint, int, int)[]>(nameof(SystemdServiceProperties.ExecStartPreEx));
    }

    public static Task<(string, string[], bool, ulong, ulong, ulong, ulong, uint, int, int)[]> GetExecStartAsync(this ISystemdService o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<(string, string[], bool, ulong, ulong, ulong, ulong, uint, int, int)[]>(nameof(SystemdServiceProperties.ExecStart));
    }

    public static Task<(string, string[], string[], ulong, ulong, ulong, ulong, uint, int, int)[]> GetExecStartExAsync(this ISystemdService o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<(string, string[], string[], ulong, ulong, ulong, ulong, uint, int, int)[]>(nameof(SystemdServiceProperties.ExecStartEx));
    }

    public static Task<(string, string[], bool, ulong, ulong, ulong, ulong, uint, int, int)[]> GetExecStartPostAsync(this ISystemdService o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<(string, string[], bool, ulong, ulong, ulong, ulong, uint, int, int)[]>(nameof(SystemdServiceProperties.ExecStartPost));
    }

    public static Task<(string, string[], string[], ulong, ulong, ulong, ulong, uint, int, int)[]> GetExecStartPostExAsync(this ISystemdService o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<(string, string[], string[], ulong, ulong, ulong, ulong, uint, int, int)[]>(
            "ExecStartPostEx");
    }

    public static Task<(string, string[], bool, ulong, ulong, ulong, ulong, uint, int, int)[]> GetExecReloadAsync(this ISystemdService o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<(string, string[], bool, ulong, ulong, ulong, ulong, uint, int, int)[]>(nameof(SystemdServiceProperties.ExecReload));
    }

    public static Task<(string, string[], string[], ulong, ulong, ulong, ulong, uint, int, int)[]> GetExecReloadExAsync(this ISystemdService o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<(string, string[], string[], ulong, ulong, ulong, ulong, uint, int, int)[]>(nameof(SystemdServiceProperties.ExecReloadEx));
    }

    public static Task<(string, string[], bool, ulong, ulong, ulong, ulong, uint, int, int)[]> GetExecStopAsync(this ISystemdService o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<(string, string[], bool, ulong, ulong, ulong, ulong, uint, int, int)[]>(nameof(SystemdServiceProperties.ExecStop));
    }

    public static Task<(string, string[], string[], ulong, ulong, ulong, ulong, uint, int, int)[]> GetExecStopExAsync(this ISystemdService o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<(string, string[], string[], ulong, ulong, ulong, ulong, uint, int, int)[]>(nameof(SystemdServiceProperties.ExecStopEx));
    }

    public static Task<(string, string[], bool, ulong, ulong, ulong, ulong, uint, int, int)[]> GetExecStopPostAsync(this ISystemdService o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<(string, string[], bool, ulong, ulong, ulong, ulong, uint, int, int)[]>(nameof(SystemdServiceProperties.ExecStopPost));
    }

    public static Task<(string, string[], string[], ulong, ulong, ulong, ulong, uint, int, int)[]> GetExecStopPostExAsync(this ISystemdService o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<(string, string[], string[], ulong, ulong, ulong, ulong, uint, int, int)[]>(nameof(SystemdServiceProperties.ExecStopPostEx));
    }

    public static Task<string> GetSliceAsync(this ISystemdService o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<string>(nameof(SystemdServiceProperties.Slice));
    }

    public static Task<string> GetControlGroupAsync(this ISystemdService o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<string>(nameof(SystemdServiceProperties.ControlGroup));
    }

    public static Task<ulong> GetControlGroupIdAsync(this ISystemdService o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<ulong>(nameof(SystemdServiceProperties.ControlGroupId));
    }

    public static Task<ulong> GetMemoryCurrentAsync(this ISystemdService o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<ulong>(nameof(SystemdServiceProperties.MemoryCurrent));
    }

    public static Task<ulong> GetMemoryPeakAsync(this ISystemdService o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<ulong>(nameof(SystemdServiceProperties.MemoryPeak));
    }

    public static Task<ulong> GetMemorySwapCurrentAsync(this ISystemdService o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<ulong>(nameof(SystemdServiceProperties.MemorySwapCurrent));
    }

    public static Task<ulong> GetMemorySwapPeakAsync(this ISystemdService o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<ulong>(nameof(SystemdServiceProperties.MemorySwapPeak));
    }

    public static Task<ulong> GetMemoryZSwapCurrentAsync(this ISystemdService o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<ulong>(nameof(SystemdServiceProperties.MemoryZSwapCurrent));
    }

    public static Task<ulong> GetMemoryAvailableAsync(this ISystemdService o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<ulong>(nameof(SystemdServiceProperties.MemoryAvailable));
    }

    public static Task<ulong> GetCPUUsageNSecAsync(this ISystemdService o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<ulong>(nameof(SystemdServiceProperties.CPUUsageNSec));
    }

    public static Task<byte[]> GetEffectiveCPUsAsync(this ISystemdService o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<byte[]>(nameof(SystemdServiceProperties.EffectiveCPUs));
    }

    public static Task<byte[]> GetEffectiveMemoryNodesAsync(this ISystemdService o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<byte[]>(nameof(SystemdServiceProperties.EffectiveMemoryNodes));
    }

    public static Task<ulong> GetTasksCurrentAsync(this ISystemdService o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<ulong>(nameof(SystemdServiceProperties.TasksCurrent));
    }

    public static Task<ulong> GetIPIngressBytesAsync(this ISystemdService o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<ulong>(nameof(SystemdServiceProperties.IPIngressBytes));
    }

    public static Task<ulong> GetIPIngressPacketsAsync(this ISystemdService o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<ulong>(nameof(SystemdServiceProperties.IPIngressPackets));
    }

    public static Task<ulong> GetIPEgressBytesAsync(this ISystemdService o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<ulong>(nameof(SystemdServiceProperties.IPEgressBytes));
    }

    public static Task<ulong> GetIPEgressPacketsAsync(this ISystemdService o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<ulong>(nameof(SystemdServiceProperties.IPEgressPackets));
    }

    public static Task<ulong> GetIOReadBytesAsync(this ISystemdService o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<ulong>(nameof(SystemdServiceProperties.IOReadBytes));
    }

    public static Task<ulong> GetIOReadOperationsAsync(this ISystemdService o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<ulong>(nameof(SystemdServiceProperties.IOReadOperations));
    }

    public static Task<ulong> GetIOWriteBytesAsync(this ISystemdService o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<ulong>(nameof(SystemdServiceProperties.IOWriteBytes));
    }

    public static Task<ulong> GetIOWriteOperationsAsync(this ISystemdService o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<ulong>(nameof(SystemdServiceProperties.IOWriteOperations));
    }

    public static Task<bool> GetDelegateAsync(this ISystemdService o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<bool>(nameof(SystemdServiceProperties.Delegate));
    }

    public static Task<string[]> GetDelegateControllersAsync(this ISystemdService o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<string[]>(nameof(SystemdServiceProperties.DelegateControllers));
    }

    public static Task<string> GetDelegateSubgroupAsync(this ISystemdService o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<string>(nameof(SystemdServiceProperties.DelegateSubgroup));
    }

    public static Task<bool> GetCPUAccountingAsync(this ISystemdService o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<bool>(nameof(SystemdServiceProperties.CPUAccounting));
    }

    public static Task<ulong> GetCPUWeightAsync(this ISystemdService o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<ulong>(nameof(SystemdServiceProperties.CPUWeight));
    }

    public static Task<ulong> GetStartupCPUWeightAsync(this ISystemdService o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<ulong>(nameof(SystemdServiceProperties.StartupCPUWeight));
    }

    public static Task<ulong> GetCPUSharesAsync(this ISystemdService o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<ulong>(nameof(SystemdServiceProperties.CPUShares));
    }

    public static Task<ulong> GetStartupCPUSharesAsync(this ISystemdService o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<ulong>(nameof(SystemdServiceProperties.StartupCPUShares));
    }

    public static Task<ulong> GetCPUQuotaPerSecUSecAsync(this ISystemdService o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<ulong>(nameof(SystemdServiceProperties.CPUQuotaPerSecUSec));
    }

    public static Task<ulong> GetCPUQuotaPeriodUSecAsync(this ISystemdService o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<ulong>(nameof(SystemdServiceProperties.CPUQuotaPeriodUSec));
    }

    public static Task<byte[]> GetAllowedCPUsAsync(this ISystemdService o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<byte[]>(nameof(SystemdServiceProperties.AllowedCPUs));
    }

    public static Task<byte[]> GetStartupAllowedCPUsAsync(this ISystemdService o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<byte[]>(nameof(SystemdServiceProperties.StartupAllowedCPUs));
    }

    public static Task<byte[]> GetAllowedMemoryNodesAsync(this ISystemdService o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<byte[]>(nameof(SystemdServiceProperties.AllowedMemoryNodes));
    }

    public static Task<byte[]> GetStartupAllowedMemoryNodesAsync(this ISystemdService o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<byte[]>(nameof(SystemdServiceProperties.StartupAllowedMemoryNodes));
    }

    public static Task<bool> GetIOAccountingAsync(this ISystemdService o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<bool>(nameof(SystemdServiceProperties.IOAccounting));
    }

    public static Task<ulong> GetIOWeightAsync(this ISystemdService o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<ulong>(nameof(SystemdServiceProperties.IOWeight));
    }

    public static Task<ulong> GetStartupIOWeightAsync(this ISystemdService o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<ulong>(nameof(SystemdServiceProperties.StartupIOWeight));
    }

    public static Task<(string, ulong)[]> GetIODeviceWeightAsync(this ISystemdService o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<(string, ulong)[]>(nameof(SystemdServiceProperties.IODeviceWeight));
    }

    public static Task<(string, ulong)[]> GetIOReadBandwidthMaxAsync(this ISystemdService o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<(string, ulong)[]>(nameof(SystemdServiceProperties.IOReadBandwidthMax));
    }

    public static Task<(string, ulong)[]> GetIOWriteBandwidthMaxAsync(this ISystemdService o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<(string, ulong)[]>(nameof(SystemdServiceProperties.IOWriteBandwidthMax));
    }

    public static Task<(string, ulong)[]> GetIOReadIOPSMaxAsync(this ISystemdService o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<(string, ulong)[]>(nameof(SystemdServiceProperties.IOReadIOPSMax));
    }

    public static Task<(string, ulong)[]> GetIOWriteIOPSMaxAsync(this ISystemdService o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<(string, ulong)[]>(nameof(SystemdServiceProperties.IOWriteIOPSMax));
    }

    public static Task<(string, ulong)[]> GetIODeviceLatencyTargetUSecAsync(this ISystemdService o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<(string, ulong)[]>(nameof(SystemdServiceProperties.IODeviceLatencyTargetUSec));
    }

    public static Task<bool> GetBlockIOAccountingAsync(this ISystemdService o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<bool>(nameof(SystemdServiceProperties.BlockIOAccounting));
    }

    public static Task<ulong> GetBlockIOWeightAsync(this ISystemdService o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<ulong>(nameof(SystemdServiceProperties.BlockIOWeight));
    }

    public static Task<ulong> GetStartupBlockIOWeightAsync(this ISystemdService o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<ulong>(nameof(SystemdServiceProperties.StartupBlockIOWeight));
    }

    public static Task<(string, ulong)[]> GetBlockIODeviceWeightAsync(this ISystemdService o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<(string, ulong)[]>(nameof(SystemdServiceProperties.BlockIODeviceWeight));
    }

    public static Task<(string, ulong)[]> GetBlockIOReadBandwidthAsync(this ISystemdService o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<(string, ulong)[]>(nameof(SystemdServiceProperties.BlockIOReadBandwidth));
    }

    public static Task<(string, ulong)[]> GetBlockIOWriteBandwidthAsync(this ISystemdService o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<(string, ulong)[]>(nameof(SystemdServiceProperties.BlockIOWriteBandwidth));
    }

    public static Task<bool> GetMemoryAccountingAsync(this ISystemdService o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<bool>(nameof(SystemdServiceProperties.MemoryAccounting));
    }

    public static Task<ulong> GetDefaultMemoryLowAsync(this ISystemdService o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<ulong>(nameof(SystemdServiceProperties.DefaultMemoryLow));
    }

    public static Task<ulong> GetDefaultStartupMemoryLowAsync(this ISystemdService o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<ulong>(nameof(SystemdServiceProperties.DefaultStartupMemoryLow));
    }

    public static Task<ulong> GetDefaultMemoryMinAsync(this ISystemdService o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<ulong>(nameof(SystemdServiceProperties.DefaultMemoryMin));
    }

    public static Task<ulong> GetMemoryMinAsync(this ISystemdService o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<ulong>(nameof(SystemdServiceProperties.MemoryMin));
    }

    public static Task<ulong> GetMemoryLowAsync(this ISystemdService o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<ulong>(nameof(SystemdServiceProperties.MemoryLow));
    }

    public static Task<ulong> GetStartupMemoryLowAsync(this ISystemdService o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<ulong>(nameof(SystemdServiceProperties.StartupMemoryLow));
    }

    public static Task<ulong> GetMemoryHighAsync(this ISystemdService o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<ulong>(nameof(SystemdServiceProperties.MemoryHigh));
    }

    public static Task<ulong> GetStartupMemoryHighAsync(this ISystemdService o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<ulong>(nameof(SystemdServiceProperties.StartupMemoryHigh));
    }

    public static Task<ulong> GetMemoryMaxAsync(this ISystemdService o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<ulong>(nameof(SystemdServiceProperties.MemoryMax));
    }

    public static Task<ulong> GetStartupMemoryMaxAsync(this ISystemdService o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<ulong>(nameof(SystemdServiceProperties.StartupMemoryMax));
    }

    public static Task<ulong> GetMemorySwapMaxAsync(this ISystemdService o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<ulong>(nameof(SystemdServiceProperties.MemorySwapMax));
    }

    public static Task<ulong> GetStartupMemorySwapMaxAsync(this ISystemdService o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<ulong>(nameof(SystemdServiceProperties.StartupMemorySwapMax));
    }

    public static Task<ulong> GetMemoryZSwapMaxAsync(this ISystemdService o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<ulong>(nameof(SystemdServiceProperties.MemoryZSwapMax));
    }

    public static Task<ulong> GetStartupMemoryZSwapMaxAsync(this ISystemdService o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<ulong>(nameof(SystemdServiceProperties.StartupMemoryZSwapMax));
    }

    public static Task<ulong> GetMemoryLimitAsync(this ISystemdService o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<ulong>(nameof(SystemdServiceProperties.MemoryLimit));
    }

    public static Task<string> GetDevicePolicyAsync(this ISystemdService o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<string>(nameof(SystemdServiceProperties.DevicePolicy));
    }

    public static Task<(string, string)[]> GetDeviceAllowAsync(this ISystemdService o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<(string, string)[]>(nameof(SystemdServiceProperties.DeviceAllow));
    }

    public static Task<bool> GetTasksAccountingAsync(this ISystemdService o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<bool>(nameof(SystemdServiceProperties.TasksAccounting));
    }

    public static Task<ulong> GetTasksMaxAsync(this ISystemdService o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<ulong>(nameof(SystemdServiceProperties.TasksMax));
    }

    public static Task<bool> GetIPAccountingAsync(this ISystemdService o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<bool>(nameof(SystemdServiceProperties.IPAccounting));
    }

    public static Task<(int, byte[], uint)[]> GetIPAddressAllowAsync(this ISystemdService o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<(int, byte[], uint)[]>(nameof(SystemdServiceProperties.IPAddressAllow));
    }

    public static Task<(int, byte[], uint)[]> GetIPAddressDenyAsync(this ISystemdService o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<(int, byte[], uint)[]>(nameof(SystemdServiceProperties.IPAddressDeny));
    }

    public static Task<string[]> GetIPIngressFilterPathAsync(this ISystemdService o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<string[]>(nameof(SystemdServiceProperties.IPIngressFilterPath));
    }

    public static Task<string[]> GetIPEgressFilterPathAsync(this ISystemdService o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<string[]>(nameof(SystemdServiceProperties.IPEgressFilterPath));
    }

    public static Task<string[]> GetDisableControllersAsync(this ISystemdService o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<string[]>(nameof(SystemdServiceProperties.DisableControllers));
    }

    public static Task<string> GetManagedOOMSwapAsync(this ISystemdService o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<string>(nameof(SystemdServiceProperties.ManagedOOMSwap));
    }

    public static Task<string> GetManagedOOMMemoryPressureAsync(this ISystemdService o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<string>(nameof(SystemdServiceProperties.ManagedOOMMemoryPressure));
    }

    public static Task<uint> GetManagedOOMMemoryPressureLimitAsync(this ISystemdService o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<uint>(nameof(SystemdServiceProperties.ManagedOOMMemoryPressureLimit));
    }

    public static Task<string> GetManagedOOMPreferenceAsync(this ISystemdService o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<string>(nameof(SystemdServiceProperties.ManagedOOMPreference));
    }

    public static Task<(string, string)[]> GetBPFProgramAsync(this ISystemdService o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<(string, string)[]>(nameof(SystemdServiceProperties.BPFProgram));
    }

    public static Task<(int, int, ushort, ushort)[]> GetSocketBindAllowAsync(this ISystemdService o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<(int, int, ushort, ushort)[]>(nameof(SystemdServiceProperties.SocketBindAllow));
    }

    public static Task<(int, int, ushort, ushort)[]> GetSocketBindDenyAsync(this ISystemdService o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<(int, int, ushort, ushort)[]>(nameof(SystemdServiceProperties.SocketBindDeny));
    }

    public static Task<(bool, string[])> GetRestrictNetworkInterfacesAsync(this ISystemdService o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<(bool, string[])>(nameof(SystemdServiceProperties.RestrictNetworkInterfaces));
    }

    public static Task<string> GetMemoryPressureWatchAsync(this ISystemdService o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<string>(nameof(SystemdServiceProperties.MemoryPressureWatch));
    }

    public static Task<ulong> GetMemoryPressureThresholdUSecAsync(this ISystemdService o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<ulong>(nameof(SystemdServiceProperties.MemoryPressureThresholdUSec));
    }

    public static Task<(int, int, string, string)[]> GetNFTSetAsync(this ISystemdService o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<(int, int, string, string)[]>(nameof(SystemdServiceProperties.NFTSet));
    }

    public static Task<bool> GetCoredumpReceiveAsync(this ISystemdService o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<bool>(nameof(SystemdServiceProperties.CoredumpReceive));
    }

    public static Task<string[]> GetEnvironmentAsync(this ISystemdService o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<string[]>(nameof(SystemdServiceProperties.Environment));
    }

    public static Task<(string, bool)[]> GetEnvironmentFilesAsync(this ISystemdService o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<(string, bool)[]>(nameof(SystemdServiceProperties.EnvironmentFiles));
    }

    public static Task<string[]> GetPassEnvironmentAsync(this ISystemdService o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<string[]>(nameof(SystemdServiceProperties.PassEnvironment));
    }

    public static Task<string[]> GetUnsetEnvironmentAsync(this ISystemdService o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<string[]>(nameof(SystemdServiceProperties.UnsetEnvironment));
    }

    public static Task<uint> GetUMaskAsync(this ISystemdService o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<uint>(nameof(SystemdServiceProperties.UMask));
    }

    public static Task<ulong> GetLimitCPUAsync(this ISystemdService o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<ulong>(nameof(SystemdServiceProperties.LimitCPU));
    }

    public static Task<ulong> GetLimitCPUSoftAsync(this ISystemdService o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<ulong>(nameof(SystemdServiceProperties.LimitCPUSoft));
    }

    public static Task<ulong> GetLimitFSIZEAsync(this ISystemdService o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<ulong>(nameof(SystemdServiceProperties.LimitFSIZE));
    }

    public static Task<ulong> GetLimitFSIZESoftAsync(this ISystemdService o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<ulong>(nameof(SystemdServiceProperties.LimitFSIZESoft));
    }

    public static Task<ulong> GetLimitDATAAsync(this ISystemdService o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<ulong>(nameof(SystemdServiceProperties.LimitDATA));
    }

    public static Task<ulong> GetLimitDATASoftAsync(this ISystemdService o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<ulong>(nameof(SystemdServiceProperties.LimitDATASoft));
    }

    public static Task<ulong> GetLimitSTACKAsync(this ISystemdService o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<ulong>(nameof(SystemdServiceProperties.LimitSTACK));
    }

    public static Task<ulong> GetLimitSTACKSoftAsync(this ISystemdService o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<ulong>(nameof(SystemdServiceProperties.LimitSTACKSoft));
    }

    public static Task<ulong> GetLimitCOREAsync(this ISystemdService o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<ulong>(nameof(SystemdServiceProperties.LimitCORE));
    }

    public static Task<ulong> GetLimitCORESoftAsync(this ISystemdService o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<ulong>(nameof(SystemdServiceProperties.LimitCORESoft));
    }

    public static Task<ulong> GetLimitRSSAsync(this ISystemdService o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<ulong>(nameof(SystemdServiceProperties.LimitRSS));
    }

    public static Task<ulong> GetLimitRSSSoftAsync(this ISystemdService o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<ulong>(nameof(SystemdServiceProperties.LimitRSSSoft));
    }

    public static Task<ulong> GetLimitNOFILEAsync(this ISystemdService o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<ulong>(nameof(SystemdServiceProperties.LimitNOFILE));
    }

    public static Task<ulong> GetLimitNOFILESoftAsync(this ISystemdService o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<ulong>(nameof(SystemdServiceProperties.LimitNOFILESoft));
    }

    public static Task<ulong> GetLimitASAsync(this ISystemdService o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<ulong>(nameof(SystemdServiceProperties.LimitAS));
    }

    public static Task<ulong> GetLimitASSoftAsync(this ISystemdService o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<ulong>(nameof(SystemdServiceProperties.LimitASSoft));
    }

    public static Task<ulong> GetLimitNPROCAsync(this ISystemdService o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<ulong>(nameof(SystemdServiceProperties.LimitNPROC));
    }

    public static Task<ulong> GetLimitNPROCSoftAsync(this ISystemdService o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<ulong>(nameof(SystemdServiceProperties.LimitNPROCSoft));
    }

    public static Task<ulong> GetLimitMEMLOCKAsync(this ISystemdService o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<ulong>(nameof(SystemdServiceProperties.LimitMEMLOCK));
    }

    public static Task<ulong> GetLimitMEMLOCKSoftAsync(this ISystemdService o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<ulong>(nameof(SystemdServiceProperties.LimitMEMLOCKSoft));
    }

    public static Task<ulong> GetLimitLOCKSAsync(this ISystemdService o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<ulong>(nameof(SystemdServiceProperties.LimitLOCKS));
    }

    public static Task<ulong> GetLimitLOCKSSoftAsync(this ISystemdService o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<ulong>(nameof(SystemdServiceProperties.LimitLOCKSSoft));
    }

    public static Task<ulong> GetLimitSIGPENDINGAsync(this ISystemdService o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<ulong>(nameof(SystemdServiceProperties.LimitSIGPENDING));
    }

    public static Task<ulong> GetLimitSIGPENDINGSoftAsync(this ISystemdService o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<ulong>(nameof(SystemdServiceProperties.LimitSIGPENDINGSoft));
    }

    public static Task<ulong> GetLimitMSGQUEUEAsync(this ISystemdService o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<ulong>(nameof(SystemdServiceProperties.LimitMSGQUEUE));
    }

    public static Task<ulong> GetLimitMSGQUEUESoftAsync(this ISystemdService o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<ulong>(nameof(SystemdServiceProperties.LimitMSGQUEUESoft));
    }

    public static Task<ulong> GetLimitNICEAsync(this ISystemdService o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<ulong>(nameof(SystemdServiceProperties.LimitNICE));
    }

    public static Task<ulong> GetLimitNICESoftAsync(this ISystemdService o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<ulong>(nameof(SystemdServiceProperties.LimitNICESoft));
    }

    public static Task<ulong> GetLimitRTPRIOAsync(this ISystemdService o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<ulong>(nameof(SystemdServiceProperties.LimitRTPRIO));
    }

    public static Task<ulong> GetLimitRTPRIOSoftAsync(this ISystemdService o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<ulong>(nameof(SystemdServiceProperties.LimitRTPRIOSoft));
    }

    public static Task<ulong> GetLimitRTTIMEAsync(this ISystemdService o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<ulong>(nameof(SystemdServiceProperties.LimitRTTIME));
    }

    public static Task<ulong> GetLimitRTTIMESoftAsync(this ISystemdService o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<ulong>(nameof(SystemdServiceProperties.LimitRTTIMESoft));
    }

    public static Task<string> GetWorkingDirectoryAsync(this ISystemdService o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<string>(nameof(SystemdServiceProperties.WorkingDirectory));
    }

    public static Task<string> GetRootDirectoryAsync(this ISystemdService o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<string>(nameof(SystemdServiceProperties.RootDirectory));
    }

    public static Task<string> GetRootImageAsync(this ISystemdService o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<string>(nameof(SystemdServiceProperties.RootImage));
    }

    public static Task<(string, string)[]> GetRootImageOptionsAsync(this ISystemdService o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<(string, string)[]>(nameof(SystemdServiceProperties.RootImageOptions));
    }

    public static Task<byte[]> GetRootHashAsync(this ISystemdService o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<byte[]>(nameof(SystemdServiceProperties.RootHash));
    }

    public static Task<string> GetRootHashPathAsync(this ISystemdService o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<string>(nameof(SystemdServiceProperties.RootHashPath));
    }

    public static Task<byte[]> GetRootHashSignatureAsync(this ISystemdService o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<byte[]>(nameof(SystemdServiceProperties.RootHashSignature));
    }

    public static Task<string> GetRootHashSignaturePathAsync(this ISystemdService o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<string>(nameof(SystemdServiceProperties.RootHashSignaturePath));
    }

    public static Task<string> GetRootVerityAsync(this ISystemdService o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<string>(nameof(SystemdServiceProperties.RootVerity));
    }

    public static Task<bool> GetRootEphemeralAsync(this ISystemdService o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<bool>(nameof(SystemdServiceProperties.RootEphemeral));
    }

    public static Task<string[]> GetExtensionDirectoriesAsync(this ISystemdService o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<string[]>(nameof(SystemdServiceProperties.ExtensionDirectories));
    }

    public static Task<(string, bool, (string, string)[])[]> GetExtensionImagesAsync(this ISystemdService o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<(string, bool, (string, string)[])[]>(nameof(SystemdServiceProperties.ExtensionImages));
    }

    public static Task<(string, string, bool, (string, string)[])[]> GetMountImagesAsync(this ISystemdService o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<(string, string, bool, (string, string)[])[]>(nameof(SystemdServiceProperties.MountImages));
    }

    public static Task<int> GetOOMScoreAdjustAsync(this ISystemdService o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<int>(nameof(SystemdServiceProperties.OOMScoreAdjust));
    }

    public static Task<ulong> GetCoredumpFilterAsync(this ISystemdService o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<ulong>(nameof(SystemdServiceProperties.CoredumpFilter));
    }

    public static Task<int> GetNiceAsync(this ISystemdService o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<int>(nameof(SystemdServiceProperties.Nice));
    }

    public static Task<int> GetIOSchedulingClassAsync(this ISystemdService o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<int>(nameof(SystemdServiceProperties.IOSchedulingClass));
    }

    public static Task<int> GetIOSchedulingPriorityAsync(this ISystemdService o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<int>(nameof(SystemdServiceProperties.IOSchedulingPriority));
    }

    public static Task<int> GetCPUSchedulingPolicyAsync(this ISystemdService o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<int>(nameof(SystemdServiceProperties.CPUSchedulingPolicy));
    }

    public static Task<int> GetCPUSchedulingPriorityAsync(this ISystemdService o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<int>(nameof(SystemdServiceProperties.CPUSchedulingPriority));
    }

    public static Task<byte[]> GetCPUAffinityAsync(this ISystemdService o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<byte[]>(nameof(SystemdServiceProperties.CPUAffinity));
    }

    public static Task<bool> GetCPUAffinityFromNUMAAsync(this ISystemdService o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<bool>(nameof(SystemdServiceProperties.CPUAffinityFromNUMA));
    }

    public static Task<int> GetNUMAPolicyAsync(this ISystemdService o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<int>(nameof(SystemdServiceProperties.NUMAPolicy));
    }

    public static Task<byte[]> GetNUMAMaskAsync(this ISystemdService o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<byte[]>(nameof(SystemdServiceProperties.NUMAMask));
    }

    public static Task<ulong> GetTimerSlackNSecAsync(this ISystemdService o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<ulong>(nameof(SystemdServiceProperties.TimerSlackNSec));
    }

    public static Task<bool> GetCPUSchedulingResetOnForkAsync(this ISystemdService o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<bool>(nameof(SystemdServiceProperties.CPUSchedulingResetOnFork));
    }

    public static Task<bool> GetNonBlockingAsync(this ISystemdService o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<bool>(nameof(SystemdServiceProperties.NonBlocking));
    }

    public static Task<string> GetStandardInputAsync(this ISystemdService o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<string>(nameof(SystemdServiceProperties.StandardInput));
    }

    public static Task<string> GetStandardInputFileDescriptorNameAsync(this ISystemdService o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<string>(nameof(SystemdServiceProperties.StandardInputFileDescriptorName));
    }

    public static Task<byte[]> GetStandardInputDataAsync(this ISystemdService o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<byte[]>(nameof(SystemdServiceProperties.StandardInputData));
    }

    public static Task<string> GetStandardOutputAsync(this ISystemdService o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<string>(nameof(SystemdServiceProperties.StandardOutput));
    }

    public static Task<string> GetStandardOutputFileDescriptorNameAsync(this ISystemdService o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<string>(nameof(SystemdServiceProperties.StandardOutputFileDescriptorName));
    }

    public static Task<string> GetStandardErrorAsync(this ISystemdService o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<string>(nameof(SystemdServiceProperties.StandardError));
    }

    public static Task<string> GetStandardErrorFileDescriptorNameAsync(this ISystemdService o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<string>(nameof(SystemdServiceProperties.StandardErrorFileDescriptorName));
    }

    public static Task<string> GetTTYPathAsync(this ISystemdService o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<string>(nameof(SystemdServiceProperties.TTYPath));
    }

    public static Task<bool> GetTTYResetAsync(this ISystemdService o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<bool>(nameof(SystemdServiceProperties.TTYReset));
    }

    public static Task<bool> GetTTYVHangupAsync(this ISystemdService o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<bool>(nameof(SystemdServiceProperties.TTYVHangup));
    }

    public static Task<bool> GetTTYVTDisallocateAsync(this ISystemdService o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<bool>(nameof(SystemdServiceProperties.TTYVTDisallocate));
    }

    public static Task<ushort> GetTTYRowsAsync(this ISystemdService o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<ushort>(nameof(SystemdServiceProperties.TTYRows));
    }

    public static Task<ushort> GetTTYColumnsAsync(this ISystemdService o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<ushort>(nameof(SystemdServiceProperties.TTYColumns));
    }

    public static Task<int> GetSyslogPriorityAsync(this ISystemdService o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<int>(nameof(SystemdServiceProperties.SyslogPriority));
    }

    public static Task<string> GetSyslogIdentifierAsync(this ISystemdService o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<string>(nameof(SystemdServiceProperties.SyslogIdentifier));
    }

    public static Task<bool> GetSyslogLevelPrefixAsync(this ISystemdService o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<bool>(nameof(SystemdServiceProperties.SyslogLevelPrefix));
    }

    public static Task<int> GetSyslogLevelAsync(this ISystemdService o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<int>(nameof(SystemdServiceProperties.SyslogLevel));
    }

    public static Task<int> GetSyslogFacilityAsync(this ISystemdService o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<int>(nameof(SystemdServiceProperties.SyslogFacility));
    }

    public static Task<int> GetLogLevelMaxAsync(this ISystemdService o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<int>(nameof(SystemdServiceProperties.LogLevelMax));
    }

    public static Task<ulong> GetLogRateLimitIntervalUSecAsync(this ISystemdService o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<ulong>(nameof(SystemdServiceProperties.LogRateLimitIntervalUSec));
    }

    public static Task<uint> GetLogRateLimitBurstAsync(this ISystemdService o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<uint>(nameof(SystemdServiceProperties.LogRateLimitBurst));
    }

    public static Task<byte[][]> GetLogExtraFieldsAsync(this ISystemdService o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<byte[][]>(nameof(SystemdServiceProperties.LogExtraFields));
    }

    public static Task<(bool, string)[]> GetLogFilterPatternsAsync(this ISystemdService o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<(bool, string)[]>(nameof(SystemdServiceProperties.LogFilterPatterns));
    }

    public static Task<string> GetLogNamespaceAsync(this ISystemdService o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<string>(nameof(SystemdServiceProperties.LogNamespace));
    }

    public static Task<int> GetSecureBitsAsync(this ISystemdService o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<int>(nameof(SystemdServiceProperties.SecureBits));
    }

    public static Task<ulong> GetCapabilityBoundingSetAsync(this ISystemdService o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<ulong>(nameof(SystemdServiceProperties.CapabilityBoundingSet));
    }

    public static Task<ulong> GetAmbientCapabilitiesAsync(this ISystemdService o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<ulong>(nameof(SystemdServiceProperties.AmbientCapabilities));
    }

    public static Task<string> GetUserAsync(this ISystemdService o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<string>(nameof(SystemdServiceProperties.User));
    }

    public static Task<string> GetGroupAsync(this ISystemdService o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<string>(nameof(SystemdServiceProperties.Group));
    }

    public static Task<bool> GetDynamicUserAsync(this ISystemdService o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<bool>(nameof(SystemdServiceProperties.DynamicUser));
    }

    public static Task<bool> GetSetLoginEnvironmentAsync(this ISystemdService o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<bool>(nameof(SystemdServiceProperties.SetLoginEnvironment));
    }

    public static Task<bool> GetRemoveIPCAsync(this ISystemdService o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<bool>(nameof(SystemdServiceProperties.RemoveIPC));
    }

    public static Task<(string, byte[])[]> GetSetCredentialAsync(this ISystemdService o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<(string, byte[])[]>(nameof(SystemdServiceProperties.SetCredential));
    }

    public static Task<(string, byte[])[]> GetSetCredentialEncryptedAsync(this ISystemdService o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<(string, byte[])[]>(nameof(SystemdServiceProperties.SetCredentialEncrypted));
    }

    public static Task<(string, string)[]> GetLoadCredentialAsync(this ISystemdService o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<(string, string)[]>(nameof(SystemdServiceProperties.LoadCredential));
    }

    public static Task<(string, string)[]> GetLoadCredentialEncryptedAsync(this ISystemdService o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<(string, string)[]>(nameof(SystemdServiceProperties.LoadCredentialEncrypted));
    }

    public static Task<string[]> GetImportCredentialAsync(this ISystemdService o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<string[]>(nameof(SystemdServiceProperties.ImportCredential));
    }

    public static Task<string[]> GetSupplementaryGroupsAsync(this ISystemdService o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<string[]>(nameof(SystemdServiceProperties.SupplementaryGroups));
    }

    public static Task<string> GetPAMNameAsync(this ISystemdService o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<string>(nameof(SystemdServiceProperties.PAMName));
    }

    public static Task<string[]> GetReadWritePathsAsync(this ISystemdService o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<string[]>(nameof(SystemdServiceProperties.ReadWritePaths));
    }

    public static Task<string[]> GetReadOnlyPathsAsync(this ISystemdService o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<string[]>(nameof(SystemdServiceProperties.ReadOnlyPaths));
    }

    public static Task<string[]> GetInaccessiblePathsAsync(this ISystemdService o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<string[]>(nameof(SystemdServiceProperties.InaccessiblePaths));
    }

    public static Task<string[]> GetExecPathsAsync(this ISystemdService o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<string[]>(nameof(SystemdServiceProperties.ExecPaths));
    }

    public static Task<string[]> GetNoExecPathsAsync(this ISystemdService o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<string[]>(nameof(SystemdServiceProperties.NoExecPaths));
    }

    public static Task<string[]> GetExecSearchPathAsync(this ISystemdService o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<string[]>(nameof(SystemdServiceProperties.ExecSearchPath));
    }

    public static Task<ulong> GetMountFlagsAsync(this ISystemdService o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<ulong>(nameof(SystemdServiceProperties.MountFlags));
    }

    public static Task<bool> GetPrivateTmpAsync(this ISystemdService o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<bool>(nameof(SystemdServiceProperties.PrivateTmp));
    }

    public static Task<bool> GetPrivateDevicesAsync(this ISystemdService o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<bool>(nameof(SystemdServiceProperties.PrivateDevices));
    }

    public static Task<bool> GetProtectClockAsync(this ISystemdService o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<bool>(nameof(SystemdServiceProperties.ProtectClock));
    }

    public static Task<bool> GetProtectKernelTunablesAsync(this ISystemdService o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<bool>(nameof(SystemdServiceProperties.ProtectKernelTunables));
    }

    public static Task<bool> GetProtectKernelModulesAsync(this ISystemdService o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<bool>(nameof(SystemdServiceProperties.ProtectKernelModules));
    }

    public static Task<bool> GetProtectKernelLogsAsync(this ISystemdService o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<bool>(nameof(SystemdServiceProperties.ProtectKernelLogs));
    }

    public static Task<bool> GetProtectControlGroupsAsync(this ISystemdService o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<bool>(nameof(SystemdServiceProperties.ProtectControlGroups));
    }

    public static Task<bool> GetPrivateNetworkAsync(this ISystemdService o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<bool>(nameof(SystemdServiceProperties.PrivateNetwork));
    }

    public static Task<bool> GetPrivateUsersAsync(this ISystemdService o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<bool>(nameof(SystemdServiceProperties.PrivateUsers));
    }

    public static Task<bool> GetPrivateMountsAsync(this ISystemdService o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<bool>(nameof(SystemdServiceProperties.PrivateMounts));
    }

    public static Task<bool> GetPrivateIPCAsync(this ISystemdService o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<bool>(nameof(SystemdServiceProperties.PrivateIPC));
    }

    public static Task<string> GetProtectHomeAsync(this ISystemdService o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<string>(nameof(SystemdServiceProperties.ProtectHome));
    }

    public static Task<string> GetProtectSystemAsync(this ISystemdService o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<string>(nameof(SystemdServiceProperties.ProtectSystem));
    }

    public static Task<bool> GetSameProcessGroupAsync(this ISystemdService o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<bool>(nameof(SystemdServiceProperties.SameProcessGroup));
    }

    public static Task<string> GetUtmpIdentifierAsync(this ISystemdService o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<string>(nameof(SystemdServiceProperties.UtmpIdentifier));
    }

    public static Task<string> GetUtmpModeAsync(this ISystemdService o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<string>(nameof(SystemdServiceProperties.UtmpMode));
    }

    public static Task<(bool, string)> GetSELinuxContextAsync(this ISystemdService o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<(bool, string)>(nameof(SystemdServiceProperties.SELinuxContext));
    }

    public static Task<(bool, string)> GetAppArmorProfileAsync(this ISystemdService o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<(bool, string)>(nameof(SystemdServiceProperties.AppArmorProfile));
    }

    public static Task<(bool, string)> GetSmackProcessLabelAsync(this ISystemdService o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<(bool, string)>(nameof(SystemdServiceProperties.SmackProcessLabel));
    }

    public static Task<bool> GetIgnoreSIGPIPEAsync(this ISystemdService o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<bool>(nameof(SystemdServiceProperties.IgnoreSIGPIPE));
    }

    public static Task<bool> GetNoNewPrivilegesAsync(this ISystemdService o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<bool>(nameof(SystemdServiceProperties.NoNewPrivileges));
    }

    public static Task<(bool, string[])> GetSystemCallFilterAsync(this ISystemdService o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<(bool, string[])>(nameof(SystemdServiceProperties.SystemCallFilter));
    }

    public static Task<string[]> GetSystemCallArchitecturesAsync(this ISystemdService o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<string[]>(nameof(SystemdServiceProperties.SystemCallArchitectures));
    }

    public static Task<int> GetSystemCallErrorNumberAsync(this ISystemdService o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<int>(nameof(SystemdServiceProperties.SystemCallErrorNumber));
    }

    public static Task<(bool, string[])> GetSystemCallLogAsync(this ISystemdService o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<(bool, string[])>(nameof(SystemdServiceProperties.SystemCallLog));
    }

    public static Task<string> GetPersonalityAsync(this ISystemdService o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<string>(nameof(SystemdServiceProperties.Personality));
    }

    public static Task<bool> GetLockPersonalityAsync(this ISystemdService o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<bool>(nameof(SystemdServiceProperties.LockPersonality));
    }

    public static Task<(bool, string[])> GetRestrictAddressFamiliesAsync(this ISystemdService o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<(bool, string[])>(nameof(SystemdServiceProperties.RestrictAddressFamilies));
    }

    public static Task<(string, string, ulong)[]> GetRuntimeDirectorySymlinkAsync(this ISystemdService o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<(string, string, ulong)[]>(nameof(SystemdServiceProperties.RuntimeDirectorySymlink));
    }

    public static Task<string> GetRuntimeDirectoryPreserveAsync(this ISystemdService o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<string>(nameof(SystemdServiceProperties.RuntimeDirectoryPreserve));
    }

    public static Task<uint> GetRuntimeDirectoryModeAsync(this ISystemdService o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<uint>(nameof(SystemdServiceProperties.RuntimeDirectoryMode));
    }

    public static Task<string[]> GetRuntimeDirectoryAsync(this ISystemdService o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<string[]>(nameof(SystemdServiceProperties.RuntimeDirectory));
    }

    public static Task<(string, string, ulong)[]> GetStateDirectorySymlinkAsync(this ISystemdService o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<(string, string, ulong)[]>(nameof(SystemdServiceProperties.StateDirectorySymlink));
    }

    public static Task<uint> GetStateDirectoryModeAsync(this ISystemdService o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<uint>(nameof(SystemdServiceProperties.StateDirectoryMode));
    }

    public static Task<string[]> GetStateDirectoryAsync(this ISystemdService o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<string[]>(nameof(SystemdServiceProperties.StateDirectory));
    }

    public static Task<(string, string, ulong)[]> GetCacheDirectorySymlinkAsync(this ISystemdService o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<(string, string, ulong)[]>(nameof(SystemdServiceProperties.CacheDirectorySymlink));
    }

    public static Task<uint> GetCacheDirectoryModeAsync(this ISystemdService o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<uint>(nameof(SystemdServiceProperties.CacheDirectoryMode));
    }

    public static Task<string[]> GetCacheDirectoryAsync(this ISystemdService o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<string[]>(nameof(SystemdServiceProperties.CacheDirectory));
    }

    public static Task<(string, string, ulong)[]> GetLogsDirectorySymlinkAsync(this ISystemdService o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<(string, string, ulong)[]>(nameof(SystemdServiceProperties.LogsDirectorySymlink));
    }

    public static Task<uint> GetLogsDirectoryModeAsync(this ISystemdService o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<uint>(nameof(SystemdServiceProperties.LogsDirectoryMode));
    }

    public static Task<string[]> GetLogsDirectoryAsync(this ISystemdService o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<string[]>(nameof(SystemdServiceProperties.LogsDirectory));
    }

    public static Task<uint> GetConfigurationDirectoryModeAsync(this ISystemdService o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<uint>(nameof(SystemdServiceProperties.ConfigurationDirectoryMode));
    }

    public static Task<string[]> GetConfigurationDirectoryAsync(this ISystemdService o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<string[]>(nameof(SystemdServiceProperties.ConfigurationDirectory));
    }

    public static Task<ulong> GetTimeoutCleanUSecAsync(this ISystemdService o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<ulong>(nameof(SystemdServiceProperties.TimeoutCleanUSec));
    }

    public static Task<bool> GetMemoryDenyWriteExecuteAsync(this ISystemdService o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<bool>(nameof(SystemdServiceProperties.MemoryDenyWriteExecute));
    }

    public static Task<bool> GetRestrictRealtimeAsync(this ISystemdService o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<bool>(nameof(SystemdServiceProperties.RestrictRealtime));
    }

    public static Task<bool> GetRestrictSUIDSGIDAsync(this ISystemdService o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<bool>(nameof(SystemdServiceProperties.RestrictSUIDSGID));
    }

    public static Task<ulong> GetRestrictNamespacesAsync(this ISystemdService o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<ulong>(nameof(SystemdServiceProperties.RestrictNamespaces));
    }

    public static Task<(bool, string[])> GetRestrictFileSystemsAsync(this ISystemdService o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<(bool, string[])>(nameof(SystemdServiceProperties.RestrictFileSystems));
    }

    public static Task<(string, string, bool, ulong)[]> GetBindPathsAsync(this ISystemdService o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<(string, string, bool, ulong)[]>(nameof(SystemdServiceProperties.BindPaths));
    }

    public static Task<(string, string, bool, ulong)[]> GetBindReadOnlyPathsAsync(this ISystemdService o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<(string, string, bool, ulong)[]>(nameof(SystemdServiceProperties.BindReadOnlyPaths));
    }

    public static Task<(string, string)[]> GetTemporaryFileSystemAsync(this ISystemdService o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<(string, string)[]>(nameof(SystemdServiceProperties.TemporaryFileSystem));
    }

    public static Task<bool> GetMountAPIVFSAsync(this ISystemdService o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<bool>(nameof(SystemdServiceProperties.MountAPIVFS));
    }

    public static Task<string> GetKeyringModeAsync(this ISystemdService o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<string>(nameof(SystemdServiceProperties.KeyringMode));
    }

    public static Task<string> GetProtectProcAsync(this ISystemdService o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<string>(nameof(SystemdServiceProperties.ProtectProc));
    }

    public static Task<string> GetProcSubsetAsync(this ISystemdService o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<string>(nameof(SystemdServiceProperties.ProcSubset));
    }

    public static Task<bool> GetProtectHostnameAsync(this ISystemdService o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<bool>(nameof(SystemdServiceProperties.ProtectHostname));
    }

    public static Task<bool> GetMemoryKSMAsync(this ISystemdService o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<bool>(nameof(SystemdServiceProperties.MemoryKSM));
    }

    public static Task<string> GetNetworkNamespacePathAsync(this ISystemdService o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<string>(nameof(SystemdServiceProperties.NetworkNamespacePath));
    }

    public static Task<string> GetIPCNamespacePathAsync(this ISystemdService o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<string>(nameof(SystemdServiceProperties.IPCNamespacePath));
    }

    public static Task<string> GetRootImagePolicyAsync(this ISystemdService o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<string>(nameof(SystemdServiceProperties.RootImagePolicy));
    }

    public static Task<string> GetMountImagePolicyAsync(this ISystemdService o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<string>(nameof(SystemdServiceProperties.MountImagePolicy));
    }

    public static Task<string> GetExtensionImagePolicyAsync(this ISystemdService o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<string>(nameof(SystemdServiceProperties.ExtensionImagePolicy));
    }

    public static Task<string> GetKillModeAsync(this ISystemdService o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<string>(nameof(SystemdServiceProperties.KillMode));
    }

    public static Task<int> GetKillSignalAsync(this ISystemdService o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<int>(nameof(SystemdServiceProperties.KillSignal));
    }

    public static Task<int> GetRestartKillSignalAsync(this ISystemdService o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<int>(nameof(SystemdServiceProperties.RestartKillSignal));
    }

    public static Task<int> GetFinalKillSignalAsync(this ISystemdService o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<int>(nameof(SystemdServiceProperties.FinalKillSignal));
    }

    public static Task<bool> GetSendSIGKILLAsync(this ISystemdService o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<bool>(nameof(SystemdServiceProperties.SendSIGKILL));
    }

    public static Task<bool> GetSendSIGHUPAsync(this ISystemdService o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<bool>(nameof(SystemdServiceProperties.SendSIGHUP));
    }

    public static Task<int> GetWatchdogSignalAsync(this ISystemdService o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<int>(nameof(SystemdServiceProperties.WatchdogSignal));
    }
}
