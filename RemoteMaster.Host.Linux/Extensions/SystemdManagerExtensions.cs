// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using RemoteMaster.Host.Linux.Abstractions;
using RemoteMaster.Host.Linux.Models;

namespace RemoteMaster.Host.Linux.Extensions;

public static class SystemdManagerExtensions
{
    public static Task<string> GetVersionAsync(this ISystemdManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<string>(nameof(SystemdManagerProperties.Version));
    }

    public static Task<string> GetFeaturesAsync(this ISystemdManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<string>(nameof(SystemdManagerProperties.Features));
    }

    public static Task<string> GetVirtualizationAsync(this ISystemdManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<string>(nameof(SystemdManagerProperties.Virtualization));
    }

    public static Task<string> GetConfidentialVirtualizationAsync(this ISystemdManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<string>(nameof(SystemdManagerProperties.ConfidentialVirtualization));
    }

    public static Task<string> GetArchitectureAsync(this ISystemdManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<string>(nameof(SystemdManagerProperties.Architecture));
    }

    public static Task<string> GetTaintedAsync(this ISystemdManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<string>(nameof(SystemdManagerProperties.Tainted));
    }

    public static Task<ulong> GetFirmwareTimestampAsync(this ISystemdManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<ulong>(nameof(SystemdManagerProperties.FirmwareTimestamp));
    }

    public static Task<ulong> GetFirmwareTimestampMonotonicAsync(this ISystemdManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<ulong>(nameof(SystemdManagerProperties.FirmwareTimestampMonotonic));
    }

    public static Task<ulong> GetLoaderTimestampAsync(this ISystemdManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<ulong>(nameof(SystemdManagerProperties.LoaderTimestamp));
    }

    public static Task<ulong> GetLoaderTimestampMonotonicAsync(this ISystemdManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<ulong>(nameof(SystemdManagerProperties.LoaderTimestampMonotonic));
    }

    public static Task<ulong> GetKernelTimestampAsync(this ISystemdManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<ulong>(nameof(SystemdManagerProperties.KernelTimestamp));
    }

    public static Task<ulong> GetKernelTimestampMonotonicAsync(this ISystemdManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<ulong>(nameof(SystemdManagerProperties.KernelTimestampMonotonic));
    }

    public static Task<ulong> GetInitRDTimestampAsync(this ISystemdManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<ulong>(nameof(SystemdManagerProperties.InitRDTimestamp));
    }

    public static Task<ulong> GetInitRDTimestampMonotonicAsync(this ISystemdManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<ulong>(nameof(SystemdManagerProperties.InitRDTimestampMonotonic));
    }

    public static Task<ulong> GetUserspaceTimestampAsync(this ISystemdManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<ulong>(nameof(SystemdManagerProperties.UserspaceTimestamp));
    }

    public static Task<ulong> GetUserspaceTimestampMonotonicAsync(this ISystemdManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<ulong>(nameof(SystemdManagerProperties.UserspaceTimestampMonotonic));
    }

    public static Task<ulong> GetFinishTimestampAsync(this ISystemdManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<ulong>(nameof(SystemdManagerProperties.FinishTimestamp));
    }

    public static Task<ulong> GetFinishTimestampMonotonicAsync(this ISystemdManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<ulong>(nameof(SystemdManagerProperties.FinishTimestampMonotonic));
    }

    public static Task<ulong> GetSecurityStartTimestampAsync(this ISystemdManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<ulong>(nameof(SystemdManagerProperties.SecurityStartTimestamp));
    }

    public static Task<ulong> GetSecurityStartTimestampMonotonicAsync(this ISystemdManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<ulong>(nameof(SystemdManagerProperties.SecurityStartTimestampMonotonic));
    }

    public static Task<ulong> GetSecurityFinishTimestampAsync(this ISystemdManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<ulong>(nameof(SystemdManagerProperties.SecurityFinishTimestamp));
    }

    public static Task<ulong> GetSecurityFinishTimestampMonotonicAsync(this ISystemdManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<ulong>(nameof(SystemdManagerProperties.SecurityFinishTimestampMonotonic));
    }

    public static Task<ulong> GetGeneratorsStartTimestampAsync(this ISystemdManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<ulong>(nameof(SystemdManagerProperties.GeneratorsStartTimestamp));
    }

    public static Task<ulong> GetGeneratorsStartTimestampMonotonicAsync(this ISystemdManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<ulong>(nameof(SystemdManagerProperties.GeneratorsStartTimestampMonotonic));
    }

    public static Task<ulong> GetGeneratorsFinishTimestampAsync(this ISystemdManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<ulong>(nameof(SystemdManagerProperties.GeneratorsFinishTimestamp));
    }

    public static Task<ulong> GetGeneratorsFinishTimestampMonotonicAsync(this ISystemdManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<ulong>(nameof(SystemdManagerProperties.GeneratorsFinishTimestampMonotonic));
    }

    public static Task<ulong> GetUnitsLoadStartTimestampAsync(this ISystemdManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<ulong>(nameof(SystemdManagerProperties.UnitsLoadStartTimestamp));
    }

    public static Task<ulong> GetUnitsLoadStartTimestampMonotonicAsync(this ISystemdManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<ulong>(nameof(SystemdManagerProperties.UnitsLoadStartTimestampMonotonic));
    }

    public static Task<ulong> GetUnitsLoadFinishTimestampAsync(this ISystemdManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<ulong>(nameof(SystemdManagerProperties.UnitsLoadFinishTimestamp));
    }

    public static Task<ulong> GetUnitsLoadFinishTimestampMonotonicAsync(this ISystemdManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<ulong>(nameof(SystemdManagerProperties.UnitsLoadFinishTimestampMonotonic));
    }

    public static Task<ulong> GetUnitsLoadTimestampAsync(this ISystemdManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<ulong>(nameof(SystemdManagerProperties.UnitsLoadTimestamp));
    }

    public static Task<ulong> GetUnitsLoadTimestampMonotonicAsync(this ISystemdManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<ulong>(nameof(SystemdManagerProperties.UnitsLoadTimestampMonotonic));
    }

    public static Task<ulong> GetInitRDSecurityStartTimestampAsync(this ISystemdManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<ulong>(nameof(SystemdManagerProperties.InitRDSecurityStartTimestamp));
    }

    public static Task<ulong> GetInitRDSecurityStartTimestampMonotonicAsync(this ISystemdManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<ulong>(nameof(SystemdManagerProperties.InitRDSecurityStartTimestampMonotonic));
    }

    public static Task<ulong> GetInitRDSecurityFinishTimestampAsync(this ISystemdManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<ulong>(nameof(SystemdManagerProperties.InitRDSecurityFinishTimestamp));
    }

    public static Task<ulong> GetInitRDSecurityFinishTimestampMonotonicAsync(this ISystemdManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<ulong>(nameof(SystemdManagerProperties.InitRDSecurityFinishTimestampMonotonic));
    }

    public static Task<ulong> GetInitRDGeneratorsStartTimestampAsync(this ISystemdManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<ulong>(nameof(SystemdManagerProperties.InitRDGeneratorsStartTimestamp));
    }

    public static Task<ulong> GetInitRDGeneratorsStartTimestampMonotonicAsync(this ISystemdManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<ulong>(nameof(SystemdManagerProperties.InitRDGeneratorsStartTimestampMonotonic));
    }

    public static Task<ulong> GetInitRDGeneratorsFinishTimestampAsync(this ISystemdManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<ulong>(nameof(SystemdManagerProperties.InitRDGeneratorsFinishTimestamp));
    }

    public static Task<ulong> GetInitRDGeneratorsFinishTimestampMonotonicAsync(this ISystemdManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<ulong>(nameof(SystemdManagerProperties.InitRDGeneratorsFinishTimestampMonotonic));
    }

    public static Task<ulong> GetInitRDUnitsLoadStartTimestampAsync(this ISystemdManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<ulong>(nameof(SystemdManagerProperties.InitRDUnitsLoadStartTimestamp));
    }

    public static Task<ulong> GetInitRDUnitsLoadStartTimestampMonotonicAsync(this ISystemdManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<ulong>(nameof(SystemdManagerProperties.InitRDUnitsLoadStartTimestampMonotonic));
    }

    public static Task<ulong> GetInitRDUnitsLoadFinishTimestampAsync(this ISystemdManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<ulong>(nameof(SystemdManagerProperties.InitRDUnitsLoadFinishTimestamp));
    }

    public static Task<ulong> GetInitRDUnitsLoadFinishTimestampMonotonicAsync(this ISystemdManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<ulong>(nameof(SystemdManagerProperties.InitRDUnitsLoadFinishTimestampMonotonic));
    }

    public static Task<string> GetLogLevelAsync(this ISystemdManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<string>(nameof(SystemdManagerProperties.LogLevel));
    }

    public static Task<string> GetLogTargetAsync(this ISystemdManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<string>(nameof(SystemdManagerProperties.LogTarget));
    }

    public static Task<uint> GetNNamesAsync(this ISystemdManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<uint>(nameof(SystemdManagerProperties.NNames));
    }

    public static Task<uint> GetNFailedUnitsAsync(this ISystemdManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<uint>(nameof(SystemdManagerProperties.NFailedUnits));
    }

    public static Task<uint> GetNJobsAsync(this ISystemdManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<uint>(nameof(SystemdManagerProperties.NJobs));
    }

    public static Task<uint> GetNInstalledJobsAsync(this ISystemdManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<uint>(nameof(SystemdManagerProperties.NInstalledJobs));
    }

    public static Task<uint> GetNFailedJobsAsync(this ISystemdManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<uint>(nameof(SystemdManagerProperties.NFailedJobs));
    }

    public static Task<double> GetProgressAsync(this ISystemdManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<double>(nameof(SystemdManagerProperties.Progress));
    }

    public static Task<string[]> GetEnvironmentAsync(this ISystemdManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<string[]>(nameof(SystemdManagerProperties.Environment));
    }

    public static Task<bool> GetConfirmSpawnAsync(this ISystemdManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<bool>(nameof(SystemdManagerProperties.ConfirmSpawn));
    }

    public static Task<bool> GetShowStatusAsync(this ISystemdManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<bool>(nameof(SystemdManagerProperties.ShowStatus));
    }

    public static Task<string[]> GetUnitPathAsync(this ISystemdManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<string[]>(nameof(SystemdManagerProperties.UnitPath));
    }

    public static Task<string> GetDefaultStandardOutputAsync(this ISystemdManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<string>(nameof(SystemdManagerProperties.DefaultStandardOutput));
    }

    public static Task<string> GetDefaultStandardErrorAsync(this ISystemdManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<string>(nameof(SystemdManagerProperties.DefaultStandardError));
    }

    public static Task<string> GetWatchdogDeviceAsync(this ISystemdManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<string>(nameof(SystemdManagerProperties.WatchdogDevice));
    }

    public static Task<ulong> GetWatchdogLastPingTimestampAsync(this ISystemdManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<ulong>(nameof(SystemdManagerProperties.WatchdogLastPingTimestamp));
    }

    public static Task<ulong> GetWatchdogLastPingTimestampMonotonicAsync(this ISystemdManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<ulong>(nameof(SystemdManagerProperties.WatchdogLastPingTimestampMonotonic));
    }

    public static Task<ulong> GetRuntimeWatchdogUSecAsync(this ISystemdManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<ulong>(nameof(SystemdManagerProperties.RuntimeWatchdogUSec));
    }

    public static Task<ulong> GetRuntimeWatchdogPreUSecAsync(this ISystemdManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<ulong>(nameof(SystemdManagerProperties.RuntimeWatchdogPreUSec));
    }

    public static Task<string> GetRuntimeWatchdogPreGovernorAsync(this ISystemdManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<string>(nameof(SystemdManagerProperties.RuntimeWatchdogPreGovernor));
    }

    public static Task<ulong> GetRebootWatchdogUSecAsync(this ISystemdManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<ulong>(nameof(SystemdManagerProperties.RebootWatchdogUSec));
    }

    public static Task<ulong> GetKExecWatchdogUSecAsync(this ISystemdManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<ulong>(nameof(SystemdManagerProperties.KExecWatchdogUSec));
    }

    public static Task<bool> GetServiceWatchdogsAsync(this ISystemdManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<bool>(nameof(SystemdManagerProperties.ServiceWatchdogs));
    }

    public static Task<string> GetControlGroupAsync(this ISystemdManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<string>(nameof(SystemdManagerProperties.ControlGroup));
    }

    public static Task<string> GetSystemStateAsync(this ISystemdManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<string>(nameof(SystemdManagerProperties.SystemState));
    }

    public static Task<byte> GetExitCodeAsync(this ISystemdManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<byte>(nameof(SystemdManagerProperties.ExitCode));
    }

    public static Task<ulong> GetDefaultTimerAccuracyUSecAsync(this ISystemdManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<ulong>(nameof(SystemdManagerProperties.DefaultTimerAccuracyUSec));
    }

    public static Task<ulong> GetDefaultTimeoutStartUSecAsync(this ISystemdManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<ulong>(nameof(SystemdManagerProperties.DefaultTimeoutStartUSec));
    }

    public static Task<ulong> GetDefaultTimeoutStopUSecAsync(this ISystemdManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<ulong>(nameof(SystemdManagerProperties.DefaultTimeoutStopUSec));
    }

    public static Task<ulong> GetDefaultTimeoutAbortUSecAsync(this ISystemdManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<ulong>(nameof(SystemdManagerProperties.DefaultTimeoutAbortUSec));
    }

    public static Task<ulong> GetDefaultDeviceTimeoutUSecAsync(this ISystemdManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<ulong>(nameof(SystemdManagerProperties.DefaultDeviceTimeoutUSec));
    }

    public static Task<ulong> GetDefaultRestartUSecAsync(this ISystemdManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<ulong>(nameof(SystemdManagerProperties.DefaultRestartUSec));
    }

    public static Task<ulong> GetDefaultStartLimitIntervalUSecAsync(this ISystemdManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<ulong>(nameof(SystemdManagerProperties.DefaultStartLimitIntervalUSec));
    }

    public static Task<uint> GetDefaultStartLimitBurstAsync(this ISystemdManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<uint>(nameof(SystemdManagerProperties.DefaultStartLimitBurst));
    }

    public static Task<bool> GetDefaultCPUAccountingAsync(this ISystemdManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<bool>(nameof(SystemdManagerProperties.DefaultCPUAccounting));
    }

    public static Task<bool> GetDefaultBlockIOAccountingAsync(this ISystemdManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<bool>(nameof(SystemdManagerProperties.DefaultBlockIOAccounting));
    }

    public static Task<bool> GetDefaultIOAccountingAsync(this ISystemdManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<bool>(nameof(SystemdManagerProperties.DefaultIOAccounting));
    }

    public static Task<bool> GetDefaultIPAccountingAsync(this ISystemdManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<bool>(nameof(SystemdManagerProperties.DefaultIPAccounting));
    }

    public static Task<bool> GetDefaultMemoryAccountingAsync(this ISystemdManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<bool>(nameof(SystemdManagerProperties.DefaultMemoryAccounting));
    }

    public static Task<bool> GetDefaultTasksAccountingAsync(this ISystemdManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<bool>(nameof(SystemdManagerProperties.DefaultTasksAccounting));
    }

    public static Task<ulong> GetDefaultLimitCPUAsync(this ISystemdManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<ulong>(nameof(SystemdManagerProperties.DefaultLimitCPU));
    }

    public static Task<ulong> GetDefaultLimitCPUSoftAsync(this ISystemdManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<ulong>(nameof(SystemdManagerProperties.DefaultLimitCPUSoft));
    }

    public static Task<ulong> GetDefaultLimitFSIZEAsync(this ISystemdManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<ulong>(nameof(SystemdManagerProperties.DefaultLimitFSIZE));
    }

    public static Task<ulong> GetDefaultLimitFSIZESoftAsync(this ISystemdManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<ulong>(nameof(SystemdManagerProperties.DefaultLimitFSIZESoft));
    }

    public static Task<ulong> GetDefaultLimitDATAAsync(this ISystemdManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<ulong>(nameof(SystemdManagerProperties.DefaultLimitDATA));
    }

    public static Task<ulong> GetDefaultLimitDATASoftAsync(this ISystemdManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<ulong>(nameof(SystemdManagerProperties.DefaultLimitDATASoft));
    }

    public static Task<ulong> GetDefaultLimitSTACKAsync(this ISystemdManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<ulong>(nameof(SystemdManagerProperties.DefaultLimitSTACK));
    }

    public static Task<ulong> GetDefaultLimitSTACKSoftAsync(this ISystemdManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<ulong>(nameof(SystemdManagerProperties.DefaultLimitSTACKSoft));
    }

    public static Task<ulong> GetDefaultLimitCOREAsync(this ISystemdManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<ulong>(nameof(SystemdManagerProperties.DefaultLimitCORE));
    }

    public static Task<ulong> GetDefaultLimitCORESoftAsync(this ISystemdManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<ulong>(nameof(SystemdManagerProperties.DefaultLimitCORESoft));
    }

    public static Task<ulong> GetDefaultLimitRSSAsync(this ISystemdManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<ulong>(nameof(SystemdManagerProperties.DefaultLimitRSS));
    }

    public static Task<ulong> GetDefaultLimitRSSSoftAsync(this ISystemdManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<ulong>(nameof(SystemdManagerProperties.DefaultLimitRSSSoft));
    }

    public static Task<ulong> GetDefaultLimitNOFILEAsync(this ISystemdManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<ulong>(nameof(SystemdManagerProperties.DefaultLimitNOFILE));
    }

    public static Task<ulong> GetDefaultLimitNOFILESoftAsync(this ISystemdManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<ulong>(nameof(SystemdManagerProperties.DefaultLimitNOFILESoft));
    }

    public static Task<ulong> GetDefaultLimitASAsync(this ISystemdManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<ulong>(nameof(SystemdManagerProperties.DefaultLimitAS));
    }

    public static Task<ulong> GetDefaultLimitASSoftAsync(this ISystemdManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<ulong>(nameof(SystemdManagerProperties.DefaultLimitASSoft));
    }

    public static Task<ulong> GetDefaultLimitNPROCAsync(this ISystemdManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<ulong>(nameof(SystemdManagerProperties.DefaultLimitNPROC));
    }

    public static Task<ulong> GetDefaultLimitNPROCSoftAsync(this ISystemdManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<ulong>(nameof(SystemdManagerProperties.DefaultLimitNPROCSoft));
    }

    public static Task<ulong> GetDefaultLimitMEMLOCKAsync(this ISystemdManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<ulong>(nameof(SystemdManagerProperties.DefaultLimitMEMLOCK));
    }

    public static Task<ulong> GetDefaultLimitMEMLOCKSoftAsync(this ISystemdManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<ulong>(nameof(SystemdManagerProperties.DefaultLimitMEMLOCKSoft));
    }

    public static Task<ulong> GetDefaultLimitLOCKSAsync(this ISystemdManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<ulong>(nameof(SystemdManagerProperties.DefaultLimitLOCKS));
    }

    public static Task<ulong> GetDefaultLimitLOCKSSoftAsync(this ISystemdManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<ulong>(nameof(SystemdManagerProperties.DefaultLimitLOCKSSoft));
    }

    public static Task<ulong> GetDefaultLimitSIGPENDINGAsync(this ISystemdManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<ulong>(nameof(SystemdManagerProperties.DefaultLimitSIGPENDING));
    }

    public static Task<ulong> GetDefaultLimitSIGPENDINGSoftAsync(this ISystemdManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<ulong>(nameof(SystemdManagerProperties.DefaultLimitSIGPENDINGSoft));
    }

    public static Task<ulong> GetDefaultLimitMSGQUEUEAsync(this ISystemdManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<ulong>(nameof(SystemdManagerProperties.DefaultLimitMSGQUEUE));
    }

    public static Task<ulong> GetDefaultLimitMSGQUEUESoftAsync(this ISystemdManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<ulong>(nameof(SystemdManagerProperties.DefaultLimitMSGQUEUESoft));
    }

    public static Task<ulong> GetDefaultLimitNICEAsync(this ISystemdManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<ulong>(nameof(SystemdManagerProperties.DefaultLimitNICE));
    }

    public static Task<ulong> GetDefaultLimitNICESoftAsync(this ISystemdManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<ulong>(nameof(SystemdManagerProperties.DefaultLimitNICESoft));
    }

    public static Task<ulong> GetDefaultLimitRTPRIOAsync(this ISystemdManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<ulong>(nameof(SystemdManagerProperties.DefaultLimitRTPRIO));
    }

    public static Task<ulong> GetDefaultLimitRTPRIOSoftAsync(this ISystemdManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<ulong>(nameof(SystemdManagerProperties.DefaultLimitRTPRIOSoft));
    }

    public static Task<ulong> GetDefaultLimitRTTIMEAsync(this ISystemdManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<ulong>(nameof(SystemdManagerProperties.DefaultLimitRTTIME));
    }

    public static Task<ulong> GetDefaultLimitRTTIMESoftAsync(this ISystemdManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<ulong>(nameof(SystemdManagerProperties.DefaultLimitRTTIMESoft));
    }

    public static Task<ulong> GetDefaultTasksMaxAsync(this ISystemdManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<ulong>(nameof(SystemdManagerProperties.DefaultTasksMax));
    }

    public static Task<ulong> GetDefaultMemoryPressureThresholdUSecAsync(this ISystemdManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<ulong>(nameof(SystemdManagerProperties.DefaultMemoryPressureThresholdUSec));
    }

    public static Task<string> GetDefaultMemoryPressureWatchAsync(this ISystemdManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<string>(nameof(SystemdManagerProperties.DefaultMemoryPressureWatch));
    }

    public static Task<ulong> GetTimerSlackNSecAsync(this ISystemdManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<ulong>(nameof(SystemdManagerProperties.TimerSlackNSec));
    }

    public static Task<string> GetDefaultOOMPolicyAsync(this ISystemdManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<string>(nameof(SystemdManagerProperties.DefaultOOMPolicy));
    }

    public static Task<int> GetDefaultOOMScoreAdjustAsync(this ISystemdManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<int>(nameof(SystemdManagerProperties.DefaultOOMScoreAdjust));
    }

    public static Task<string> GetCtrlAltDelBurstActionAsync(this ISystemdManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<string>(nameof(SystemdManagerProperties.CtrlAltDelBurstAction));
    }

    public static Task SetLogLevelAsync(this ISystemdManager o, string val)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.SetAsync(nameof(SystemdManagerProperties.LogLevel), val);
    }

    public static Task SetLogTargetAsync(this ISystemdManager o, string val)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.SetAsync(nameof(SystemdManagerProperties.LogTarget), val);
    }

    public static Task SetRuntimeWatchdogUSecAsync(this ISystemdManager o, ulong val)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.SetAsync(nameof(SystemdManagerProperties.RuntimeWatchdogUSec), val);
    }

    public static Task SetRuntimeWatchdogPreUSecAsync(this ISystemdManager o, ulong val)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.SetAsync(nameof(SystemdManagerProperties.RuntimeWatchdogPreUSec), val);
    }

    public static Task SetRuntimeWatchdogPreGovernorAsync(this ISystemdManager o, string val)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.SetAsync(nameof(SystemdManagerProperties.RuntimeWatchdogPreGovernor), val);
    }

    public static Task SetRebootWatchdogUSecAsync(this ISystemdManager o, ulong val)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.SetAsync(nameof(SystemdManagerProperties.RebootWatchdogUSec), val);
    }

    public static Task SetKExecWatchdogUSecAsync(this ISystemdManager o, ulong val)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.SetAsync(nameof(SystemdManagerProperties.KExecWatchdogUSec), val);
    }

    public static Task SetServiceWatchdogsAsync(this ISystemdManager o, bool val)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.SetAsync(nameof(SystemdManagerProperties.ServiceWatchdogs), val);
    }
}
