// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using RemoteMaster.Host.Linux.Abstractions;

namespace RemoteMaster.Host.Linux.Extensions;

public static class SystemdManagerExtensions
{
    public static Task<string> GetVersionAsync(this ISystemdManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<string>("Version");
    }

    public static Task<string> GetFeaturesAsync(this ISystemdManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<string>("Features");
    }

    public static Task<string> GetVirtualizationAsync(this ISystemdManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<string>("Virtualization");
    }

    public static Task<string> GetConfidentialVirtualizationAsync(this ISystemdManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<string>("ConfidentialVirtualization");
    }

    public static Task<string> GetArchitectureAsync(this ISystemdManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<string>("Architecture");
    }

    public static Task<string> GetTaintedAsync(this ISystemdManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<string>("Tainted");
    }

    public static Task<ulong> GetFirmwareTimestampAsync(this ISystemdManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<ulong>("FirmwareTimestamp");
    }

    public static Task<ulong> GetFirmwareTimestampMonotonicAsync(this ISystemdManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<ulong>("FirmwareTimestampMonotonic");
    }

    public static Task<ulong> GetLoaderTimestampAsync(this ISystemdManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<ulong>("LoaderTimestamp");
    }

    public static Task<ulong> GetLoaderTimestampMonotonicAsync(this ISystemdManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<ulong>("LoaderTimestampMonotonic");
    }

    public static Task<ulong> GetKernelTimestampAsync(this ISystemdManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<ulong>("KernelTimestamp");
    }

    public static Task<ulong> GetKernelTimestampMonotonicAsync(this ISystemdManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<ulong>("KernelTimestampMonotonic");
    }

    public static Task<ulong> GetInitRDTimestampAsync(this ISystemdManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<ulong>("InitRDTimestamp");
    }

    public static Task<ulong> GetInitRDTimestampMonotonicAsync(this ISystemdManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<ulong>("InitRDTimestampMonotonic");
    }

    public static Task<ulong> GetUserspaceTimestampAsync(this ISystemdManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<ulong>("UserspaceTimestamp");
    }

    public static Task<ulong> GetUserspaceTimestampMonotonicAsync(this ISystemdManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<ulong>("UserspaceTimestampMonotonic");
    }

    public static Task<ulong> GetFinishTimestampAsync(this ISystemdManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<ulong>("FinishTimestamp");
    }

    public static Task<ulong> GetFinishTimestampMonotonicAsync(this ISystemdManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<ulong>("FinishTimestampMonotonic");
    }

    public static Task<ulong> GetSecurityStartTimestampAsync(this ISystemdManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<ulong>("SecurityStartTimestamp");
    }

    public static Task<ulong> GetSecurityStartTimestampMonotonicAsync(this ISystemdManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<ulong>("SecurityStartTimestampMonotonic");
    }

    public static Task<ulong> GetSecurityFinishTimestampAsync(this ISystemdManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<ulong>("SecurityFinishTimestamp");
    }

    public static Task<ulong> GetSecurityFinishTimestampMonotonicAsync(this ISystemdManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<ulong>("SecurityFinishTimestampMonotonic");
    }

    public static Task<ulong> GetGeneratorsStartTimestampAsync(this ISystemdManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<ulong>("GeneratorsStartTimestamp");
    }

    public static Task<ulong> GetGeneratorsStartTimestampMonotonicAsync(this ISystemdManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<ulong>("GeneratorsStartTimestampMonotonic");
    }

    public static Task<ulong> GetGeneratorsFinishTimestampAsync(this ISystemdManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<ulong>("GeneratorsFinishTimestamp");
    }

    public static Task<ulong> GetGeneratorsFinishTimestampMonotonicAsync(this ISystemdManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<ulong>("GeneratorsFinishTimestampMonotonic");
    }

    public static Task<ulong> GetUnitsLoadStartTimestampAsync(this ISystemdManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<ulong>("UnitsLoadStartTimestamp");
    }

    public static Task<ulong> GetUnitsLoadStartTimestampMonotonicAsync(this ISystemdManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<ulong>("UnitsLoadStartTimestampMonotonic");
    }

    public static Task<ulong> GetUnitsLoadFinishTimestampAsync(this ISystemdManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<ulong>("UnitsLoadFinishTimestamp");
    }

    public static Task<ulong> GetUnitsLoadFinishTimestampMonotonicAsync(this ISystemdManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<ulong>("UnitsLoadFinishTimestampMonotonic");
    }

    public static Task<ulong> GetUnitsLoadTimestampAsync(this ISystemdManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<ulong>("UnitsLoadTimestamp");
    }

    public static Task<ulong> GetUnitsLoadTimestampMonotonicAsync(this ISystemdManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<ulong>("UnitsLoadTimestampMonotonic");
    }

    public static Task<ulong> GetInitRDSecurityStartTimestampAsync(this ISystemdManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<ulong>("InitRDSecurityStartTimestamp");
    }

    public static Task<ulong> GetInitRDSecurityStartTimestampMonotonicAsync(this ISystemdManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<ulong>("InitRDSecurityStartTimestampMonotonic");
    }

    public static Task<ulong> GetInitRDSecurityFinishTimestampAsync(this ISystemdManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<ulong>("InitRDSecurityFinishTimestamp");
    }

    public static Task<ulong> GetInitRDSecurityFinishTimestampMonotonicAsync(this ISystemdManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<ulong>("InitRDSecurityFinishTimestampMonotonic");
    }

    public static Task<ulong> GetInitRDGeneratorsStartTimestampAsync(this ISystemdManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<ulong>("InitRDGeneratorsStartTimestamp");
    }

    public static Task<ulong> GetInitRDGeneratorsStartTimestampMonotonicAsync(this ISystemdManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<ulong>("InitRDGeneratorsStartTimestampMonotonic");
    }

    public static Task<ulong> GetInitRDGeneratorsFinishTimestampAsync(this ISystemdManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<ulong>("InitRDGeneratorsFinishTimestamp");
    }

    public static Task<ulong> GetInitRDGeneratorsFinishTimestampMonotonicAsync(this ISystemdManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<ulong>("InitRDGeneratorsFinishTimestampMonotonic");
    }

    public static Task<ulong> GetInitRDUnitsLoadStartTimestampAsync(this ISystemdManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<ulong>("InitRDUnitsLoadStartTimestamp");
    }

    public static Task<ulong> GetInitRDUnitsLoadStartTimestampMonotonicAsync(this ISystemdManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<ulong>("InitRDUnitsLoadStartTimestampMonotonic");
    }

    public static Task<ulong> GetInitRDUnitsLoadFinishTimestampAsync(this ISystemdManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<ulong>("InitRDUnitsLoadFinishTimestamp");
    }

    public static Task<ulong> GetInitRDUnitsLoadFinishTimestampMonotonicAsync(this ISystemdManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<ulong>("InitRDUnitsLoadFinishTimestampMonotonic");
    }

    public static Task<string> GetLogLevelAsync(this ISystemdManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<string>("LogLevel");
    }

    public static Task<string> GetLogTargetAsync(this ISystemdManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<string>("LogTarget");
    }

    public static Task<uint> GetNNamesAsync(this ISystemdManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<uint>("NNames");
    }

    public static Task<uint> GetNFailedUnitsAsync(this ISystemdManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<uint>("NFailedUnits");
    }

    public static Task<uint> GetNJobsAsync(this ISystemdManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<uint>("NJobs");
    }

    public static Task<uint> GetNInstalledJobsAsync(this ISystemdManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<uint>("NInstalledJobs");
    }

    public static Task<uint> GetNFailedJobsAsync(this ISystemdManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<uint>("NFailedJobs");
    }

    public static Task<double> GetProgressAsync(this ISystemdManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<double>("Progress");
    }

    public static Task<string[]> GetEnvironmentAsync(this ISystemdManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<string[]>("Environment");
    }

    public static Task<bool> GetConfirmSpawnAsync(this ISystemdManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<bool>("ConfirmSpawn");
    }

    public static Task<bool> GetShowStatusAsync(this ISystemdManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<bool>("ShowStatus");
    }

    public static Task<string[]> GetUnitPathAsync(this ISystemdManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<string[]>("UnitPath");
    }

    public static Task<string> GetDefaultStandardOutputAsync(this ISystemdManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<string>("DefaultStandardOutput");
    }

    public static Task<string> GetDefaultStandardErrorAsync(this ISystemdManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<string>("DefaultStandardError");
    }

    public static Task<string> GetWatchdogDeviceAsync(this ISystemdManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<string>("WatchdogDevice");
    }

    public static Task<ulong> GetWatchdogLastPingTimestampAsync(this ISystemdManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<ulong>("WatchdogLastPingTimestamp");
    }

    public static Task<ulong> GetWatchdogLastPingTimestampMonotonicAsync(this ISystemdManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<ulong>("WatchdogLastPingTimestampMonotonic");
    }

    public static Task<ulong> GetRuntimeWatchdogUSecAsync(this ISystemdManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<ulong>("RuntimeWatchdogUSec");
    }

    public static Task<ulong> GetRuntimeWatchdogPreUSecAsync(this ISystemdManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<ulong>("RuntimeWatchdogPreUSec");
    }

    public static Task<string> GetRuntimeWatchdogPreGovernorAsync(this ISystemdManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<string>("RuntimeWatchdogPreGovernor");
    }

    public static Task<ulong> GetRebootWatchdogUSecAsync(this ISystemdManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<ulong>("RebootWatchdogUSec");
    }

    public static Task<ulong> GetKExecWatchdogUSecAsync(this ISystemdManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<ulong>("KExecWatchdogUSec");
    }

    public static Task<bool> GetServiceWatchdogsAsync(this ISystemdManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<bool>("ServiceWatchdogs");
    }

    public static Task<string> GetControlGroupAsync(this ISystemdManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<string>("ControlGroup");
    }

    public static Task<string> GetSystemStateAsync(this ISystemdManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<string>("SystemState");
    }

    public static Task<byte> GetExitCodeAsync(this ISystemdManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<byte>("ExitCode");
    }

    public static Task<ulong> GetDefaultTimerAccuracyUSecAsync(this ISystemdManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<ulong>("DefaultTimerAccuracyUSec");
    }

    public static Task<ulong> GetDefaultTimeoutStartUSecAsync(this ISystemdManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<ulong>("DefaultTimeoutStartUSec");
    }

    public static Task<ulong> GetDefaultTimeoutStopUSecAsync(this ISystemdManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<ulong>("DefaultTimeoutStopUSec");
    }

    public static Task<ulong> GetDefaultTimeoutAbortUSecAsync(this ISystemdManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<ulong>("DefaultTimeoutAbortUSec");
    }

    public static Task<ulong> GetDefaultDeviceTimeoutUSecAsync(this ISystemdManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<ulong>("DefaultDeviceTimeoutUSec");
    }

    public static Task<ulong> GetDefaultRestartUSecAsync(this ISystemdManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<ulong>("DefaultRestartUSec");
    }

    public static Task<ulong> GetDefaultStartLimitIntervalUSecAsync(this ISystemdManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<ulong>("DefaultStartLimitIntervalUSec");
    }

    public static Task<uint> GetDefaultStartLimitBurstAsync(this ISystemdManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<uint>("DefaultStartLimitBurst");
    }

    public static Task<bool> GetDefaultCPUAccountingAsync(this ISystemdManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<bool>("DefaultCPUAccounting");
    }

    public static Task<bool> GetDefaultBlockIOAccountingAsync(this ISystemdManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<bool>("DefaultBlockIOAccounting");
    }

    public static Task<bool> GetDefaultIOAccountingAsync(this ISystemdManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<bool>("DefaultIOAccounting");
    }

    public static Task<bool> GetDefaultIPAccountingAsync(this ISystemdManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<bool>("DefaultIPAccounting");
    }

    public static Task<bool> GetDefaultMemoryAccountingAsync(this ISystemdManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<bool>("DefaultMemoryAccounting");
    }

    public static Task<bool> GetDefaultTasksAccountingAsync(this ISystemdManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<bool>("DefaultTasksAccounting");
    }

    public static Task<ulong> GetDefaultLimitCPUAsync(this ISystemdManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<ulong>("DefaultLimitCPU");
    }

    public static Task<ulong> GetDefaultLimitCPUSoftAsync(this ISystemdManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<ulong>("DefaultLimitCPUSoft");
    }

    public static Task<ulong> GetDefaultLimitFSIZEAsync(this ISystemdManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<ulong>("DefaultLimitFSIZE");
    }

    public static Task<ulong> GetDefaultLimitFSIZESoftAsync(this ISystemdManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<ulong>("DefaultLimitFSIZESoft");
    }

    public static Task<ulong> GetDefaultLimitDATAAsync(this ISystemdManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<ulong>("DefaultLimitDATA");
    }

    public static Task<ulong> GetDefaultLimitDATASoftAsync(this ISystemdManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<ulong>("DefaultLimitDATASoft");
    }

    public static Task<ulong> GetDefaultLimitSTACKAsync(this ISystemdManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<ulong>("DefaultLimitSTACK");
    }

    public static Task<ulong> GetDefaultLimitSTACKSoftAsync(this ISystemdManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<ulong>("DefaultLimitSTACKSoft");
    }

    public static Task<ulong> GetDefaultLimitCOREAsync(this ISystemdManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<ulong>("DefaultLimitCORE");
    }

    public static Task<ulong> GetDefaultLimitCORESoftAsync(this ISystemdManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<ulong>("DefaultLimitCORESoft");
    }

    public static Task<ulong> GetDefaultLimitRSSAsync(this ISystemdManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<ulong>("DefaultLimitRSS");
    }

    public static Task<ulong> GetDefaultLimitRSSSoftAsync(this ISystemdManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<ulong>("DefaultLimitRSSSoft");
    }

    public static Task<ulong> GetDefaultLimitNOFILEAsync(this ISystemdManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<ulong>("DefaultLimitNOFILE");
    }

    public static Task<ulong> GetDefaultLimitNOFILESoftAsync(this ISystemdManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<ulong>("DefaultLimitNOFILESoft");
    }

    public static Task<ulong> GetDefaultLimitASAsync(this ISystemdManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<ulong>("DefaultLimitAS");
    }

    public static Task<ulong> GetDefaultLimitASSoftAsync(this ISystemdManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<ulong>("DefaultLimitASSoft");
    }

    public static Task<ulong> GetDefaultLimitNPROCAsync(this ISystemdManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<ulong>("DefaultLimitNPROC");
    }

    public static Task<ulong> GetDefaultLimitNPROCSoftAsync(this ISystemdManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<ulong>("DefaultLimitNPROCSoft");
    }

    public static Task<ulong> GetDefaultLimitMEMLOCKAsync(this ISystemdManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<ulong>("DefaultLimitMEMLOCK");
    }

    public static Task<ulong> GetDefaultLimitMEMLOCKSoftAsync(this ISystemdManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<ulong>("DefaultLimitMEMLOCKSoft");
    }

    public static Task<ulong> GetDefaultLimitLOCKSAsync(this ISystemdManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<ulong>("DefaultLimitLOCKS");
    }

    public static Task<ulong> GetDefaultLimitLOCKSSoftAsync(this ISystemdManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<ulong>("DefaultLimitLOCKSSoft");
    }

    public static Task<ulong> GetDefaultLimitSIGPENDINGAsync(this ISystemdManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<ulong>("DefaultLimitSIGPENDING");
    }

    public static Task<ulong> GetDefaultLimitSIGPENDINGSoftAsync(this ISystemdManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<ulong>("DefaultLimitSIGPENDINGSoft");
    }

    public static Task<ulong> GetDefaultLimitMSGQUEUEAsync(this ISystemdManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<ulong>("DefaultLimitMSGQUEUE");
    }

    public static Task<ulong> GetDefaultLimitMSGQUEUESoftAsync(this ISystemdManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<ulong>("DefaultLimitMSGQUEUESoft");
    }

    public static Task<ulong> GetDefaultLimitNICEAsync(this ISystemdManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<ulong>("DefaultLimitNICE");
    }

    public static Task<ulong> GetDefaultLimitNICESoftAsync(this ISystemdManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<ulong>("DefaultLimitNICESoft");
    }

    public static Task<ulong> GetDefaultLimitRTPRIOAsync(this ISystemdManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<ulong>("DefaultLimitRTPRIO");
    }

    public static Task<ulong> GetDefaultLimitRTPRIOSoftAsync(this ISystemdManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<ulong>("DefaultLimitRTPRIOSoft");
    }

    public static Task<ulong> GetDefaultLimitRTTIMEAsync(this ISystemdManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<ulong>("DefaultLimitRTTIME");
    }

    public static Task<ulong> GetDefaultLimitRTTIMESoftAsync(this ISystemdManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<ulong>("DefaultLimitRTTIMESoft");
    }

    public static Task<ulong> GetDefaultTasksMaxAsync(this ISystemdManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<ulong>("DefaultTasksMax");
    }

    public static Task<ulong> GetDefaultMemoryPressureThresholdUSecAsync(this ISystemdManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<ulong>("DefaultMemoryPressureThresholdUSec");
    }

    public static Task<string> GetDefaultMemoryPressureWatchAsync(this ISystemdManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<string>("DefaultMemoryPressureWatch");
    }

    public static Task<ulong> GetTimerSlackNSecAsync(this ISystemdManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<ulong>("TimerSlackNSec");
    }

    public static Task<string> GetDefaultOOMPolicyAsync(this ISystemdManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<string>("DefaultOOMPolicy");
    }

    public static Task<int> GetDefaultOOMScoreAdjustAsync(this ISystemdManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<int>("DefaultOOMScoreAdjust");
    }

    public static Task<string> GetCtrlAltDelBurstActionAsync(this ISystemdManager o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.GetAsync<string>("CtrlAltDelBurstAction");
    }

    public static Task SetLogLevelAsync(this ISystemdManager o, string val)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.SetAsync("LogLevel", val);
    }

    public static Task SetLogTargetAsync(this ISystemdManager o, string val)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.SetAsync("LogTarget", val);
    }

    public static Task SetRuntimeWatchdogUSecAsync(this ISystemdManager o, ulong val)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.SetAsync("RuntimeWatchdogUSec", val);
    }

    public static Task SetRuntimeWatchdogPreUSecAsync(this ISystemdManager o, ulong val)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.SetAsync("RuntimeWatchdogPreUSec", val);
    }

    public static Task SetRuntimeWatchdogPreGovernorAsync(this ISystemdManager o, string val)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.SetAsync("RuntimeWatchdogPreGovernor", val);
    }

    public static Task SetRebootWatchdogUSecAsync(this ISystemdManager o, ulong val)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.SetAsync("RebootWatchdogUSec", val);
    }

    public static Task SetKExecWatchdogUSecAsync(this ISystemdManager o, ulong val)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.SetAsync("KExecWatchdogUSec", val);
    }

    public static Task SetServiceWatchdogsAsync(this ISystemdManager o, bool val)
    {
        ArgumentNullException.ThrowIfNull(o);

        return o.SetAsync("ServiceWatchdogs", val);
    }
}
