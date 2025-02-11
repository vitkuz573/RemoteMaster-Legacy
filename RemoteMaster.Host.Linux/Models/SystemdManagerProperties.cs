// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using Tmds.DBus;

namespace RemoteMaster.Host.Linux.Models;

[Dictionary]
public class SystemdManagerProperties
{
    public string Version { get; set; } = string.Empty;

    public string Features { get; set; } = string.Empty;

    public string Virtualization { get; set; } = string.Empty;

    public string ConfidentialVirtualization { get; set; } = string.Empty;

    public string Architecture { get; set; } = string.Empty;

    public string Tainted { get; set; } = string.Empty;

    public ulong FirmwareTimestamp { get; set; } = default;

    public ulong FirmwareTimestampMonotonic { get; set; } = default;

    public ulong LoaderTimestamp { get; set; } = default;

    public ulong LoaderTimestampMonotonic { get; set; } = default;

    public ulong KernelTimestamp { get; set; } = default;

    public ulong KernelTimestampMonotonic { get; set; } = default;

    public ulong InitRDTimestamp { get; set; } = default;

    public ulong InitRDTimestampMonotonic { get; set; } = default;

    public ulong UserspaceTimestamp { get; set; } = default;

    public ulong UserspaceTimestampMonotonic { get; set; } = default;

    public ulong FinishTimestamp { get; set; } = default;

    public ulong FinishTimestampMonotonic { get; set; } = default;

    public ulong SecurityStartTimestamp { get; set; } = default;

    public ulong SecurityStartTimestampMonotonic { get; set; } = default;

    public ulong SecurityFinishTimestamp { get; set; } = default;

    public ulong SecurityFinishTimestampMonotonic { get; set; } = default;

    public ulong GeneratorsStartTimestamp { get; set; } = default;

    public ulong GeneratorsStartTimestampMonotonic { get; set; } = default;

    public ulong GeneratorsFinishTimestamp { get; set; } = default;

    public ulong GeneratorsFinishTimestampMonotonic { get; set; } = default;

    public ulong UnitsLoadStartTimestamp { get; set; } = default;

    public ulong UnitsLoadStartTimestampMonotonic { get; set; } = default;

    public ulong UnitsLoadFinishTimestamp { get; set; } = default;

    public ulong UnitsLoadFinishTimestampMonotonic { get; set; } = default;

    public ulong UnitsLoadTimestamp { get; set; } = default;

    public ulong UnitsLoadTimestampMonotonic { get; set; } = default;

    public ulong InitRDSecurityStartTimestamp { get; set; } = default;

    public ulong InitRDSecurityStartTimestampMonotonic { get; set; } = default;

    public ulong InitRDSecurityFinishTimestamp { get; set; } = default;

    public ulong InitRDSecurityFinishTimestampMonotonic { get; set; } = default;

    public ulong InitRDGeneratorsStartTimestamp { get; set; } = default;

    public ulong InitRDGeneratorsStartTimestampMonotonic { get; set; } = default;

    public ulong InitRDGeneratorsFinishTimestamp { get; set; } = default;

    public ulong InitRDGeneratorsFinishTimestampMonotonic { get; set; } = default;

    public ulong InitRDUnitsLoadStartTimestamp { get; set; } = default;

    public ulong InitRDUnitsLoadStartTimestampMonotonic { get; set; } = default;

    public ulong InitRDUnitsLoadFinishTimestamp { get; set; } = default;

    public ulong InitRDUnitsLoadFinishTimestampMonotonic { get; set; } = default;

    public string LogLevel { get; set; } = string.Empty;

    public string LogTarget { get; set; } = string.Empty;

    public uint NNames { get; set; } = default;

    public uint NFailedUnits { get; set; } = default;

    public uint NJobs { get; set; } = default;

    public uint NInstalledJobs { get; set; } = default;

    public uint NFailedJobs { get; set; } = default;

    public double Progress { get; set; } = default;

    public string[] Environment { get; set; } = [];

    public bool ConfirmSpawn { get; set; } = default;

    public bool ShowStatus { get; set; } = default;

    public string[] UnitPath { get; set; } = [];

    public string DefaultStandardOutput { get; set; } = string.Empty;

    public string DefaultStandardError { get; set; } = string.Empty;

    public string WatchdogDevice { get; set; } = string.Empty;

    public ulong WatchdogLastPingTimestamp { get; set; } = default;

    public ulong WatchdogLastPingTimestampMonotonic { get; set; } = default;

    public ulong RuntimeWatchdogUSec { get; set; } = default;

    public ulong RuntimeWatchdogPreUSec { get; set; } = default;

    public string RuntimeWatchdogPreGovernor { get; set; } = string.Empty;

    public ulong RebootWatchdogUSec { get; set; } = default;

    public ulong KExecWatchdogUSec { get; set; } = default;

    public bool ServiceWatchdogs { get; set; } = default;

    public string ControlGroup { get; set; } = string.Empty;

    public string SystemState { get; set; } = string.Empty;

    public byte ExitCode { get; set; } = default;

    public ulong DefaultTimerAccuracyUSec { get; set; } = default;

    public ulong DefaultTimeoutStartUSec { get; set; } = default;

    public ulong DefaultTimeoutStopUSec { get; set; } = default;

    public ulong DefaultTimeoutAbortUSec { get; set; } = default;

    public ulong DefaultDeviceTimeoutUSec { get; set; } = default;

    public ulong DefaultRestartUSec { get; set; } = default;

    public ulong DefaultStartLimitIntervalUSec { get; set; } = default;

    public uint DefaultStartLimitBurst { get; set; } = default;

    public bool DefaultCPUAccounting { get; set; } = default;

    public bool DefaultBlockIOAccounting { get; set; } = default;

    public bool DefaultIOAccounting { get; set; } = default;

    public bool DefaultIPAccounting { get; set; } = default;

    public bool DefaultMemoryAccounting { get; set; } = default;

    public bool DefaultTasksAccounting { get; set; } = default;

    public ulong DefaultLimitCPU { get; set; } = default;

    public ulong DefaultLimitCPUSoft { get; set; } = default;

    public ulong DefaultLimitFSIZE { get; set; } = default;

    public ulong DefaultLimitFSIZESoft { get; set; } = default;

    public ulong DefaultLimitDATA { get; set; } = default;

    public ulong DefaultLimitDATASoft { get; set; } = default;

    public ulong DefaultLimitSTACK { get; set; } = default;

    public ulong DefaultLimitSTACKSoft { get; set; } = default;

    public ulong DefaultLimitCORE { get; set; } = default;

    public ulong DefaultLimitCORESoft { get; set; } = default;

    public ulong DefaultLimitRSS { get; set; } = default;

    public ulong DefaultLimitRSSSoft { get; set; } = default;

    public ulong DefaultLimitNOFILE { get; set; } = default;

    public ulong DefaultLimitNOFILESoft { get; set; } = default;

    public ulong DefaultLimitAS { get; set; } = default;

    public ulong DefaultLimitASSoft { get; set; } = default;

    public ulong DefaultLimitNPROC { get; set; } = default;

    public ulong DefaultLimitNPROCSoft { get; set; } = default;

    public ulong DefaultLimitMEMLOCK { get; set; } = default;

    public ulong DefaultLimitMEMLOCKSoft { get; set; } = default;

    public ulong DefaultLimitLOCKS { get; set; } = default;

    public ulong DefaultLimitLOCKSSoft { get; set; } = default;

    public ulong DefaultLimitSIGPENDING { get; set; } = default;

    public ulong DefaultLimitSIGPENDINGSoft { get; set; } = default;

    public ulong DefaultLimitMSGQUEUE { get; set; } = default;

    public ulong DefaultLimitMSGQUEUESoft { get; set; } = default;

    public ulong DefaultLimitNICE { get; set; } = default;

    public ulong DefaultLimitNICESoft { get; set; } = default;

    public ulong DefaultLimitRTPRIO { get; set; } = default;

    public ulong DefaultLimitRTPRIOSoft { get; set; } = default;

    public ulong DefaultLimitRTTIME { get; set; } = default;

    public ulong DefaultLimitRTTIMESoft { get; set; } = default;

    public ulong DefaultTasksMax { get; set; } = default;

    public ulong DefaultMemoryPressureThresholdUSec { get; set; } = default;

    public string DefaultMemoryPressureWatch { get; set; } = string.Empty;

    public ulong TimerSlackNSec { get; set; } = default;

    public string DefaultOOMPolicy { get; set; } = string.Empty;

    public int DefaultOOMScoreAdjust { get; set; } = default;

    public string CtrlAltDelBurstAction { get; set; } = string.Empty;

}
