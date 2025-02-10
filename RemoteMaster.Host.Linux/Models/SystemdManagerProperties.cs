// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using Tmds.DBus;

namespace RemoteMaster.Host.Linux.Models;

[Dictionary]
public class SystemdManagerProperties
{
    private string _Version = string.Empty;

    public string Version
    {
        get => _Version;
        set => _Version = value;
    }

    private string _Features = string.Empty;

    public string Features
    {
        get => _Features;
        set => _Features = value;
    }

    private string _Virtualization = string.Empty;

    public string Virtualization
    {
        get => _Virtualization;
        set => _Virtualization = value;
    }

    private string _ConfidentialVirtualization = string.Empty;

    public string ConfidentialVirtualization
    {
        get => _ConfidentialVirtualization;
        set => _ConfidentialVirtualization = value;
    }

    private string _Architecture = string.Empty;

    public string Architecture
    {
        get => _Architecture;
        set => _Architecture = value;
    }

    private string _Tainted = string.Empty;

    public string Tainted
    {
        get => _Tainted;
        set => _Tainted = value;
    }

    private ulong _FirmwareTimestamp = default;

    public ulong FirmwareTimestamp
    {
        get => _FirmwareTimestamp;
        set => _FirmwareTimestamp = value;
    }

    private ulong _FirmwareTimestampMonotonic = default;

    public ulong FirmwareTimestampMonotonic
    {
        get => _FirmwareTimestampMonotonic;
        set => _FirmwareTimestampMonotonic = value;
    }

    private ulong _LoaderTimestamp = default;

    public ulong LoaderTimestamp
    {
        get => _LoaderTimestamp;
        set => _LoaderTimestamp = value;
    }

    private ulong _LoaderTimestampMonotonic = default;

    public ulong LoaderTimestampMonotonic
    {
        get => _LoaderTimestampMonotonic;
        set => _LoaderTimestampMonotonic = value;
    }

    private ulong _KernelTimestamp = default;

    public ulong KernelTimestamp
    {
        get => _KernelTimestamp;
        set => _KernelTimestamp = value;
    }

    private ulong _KernelTimestampMonotonic = default;

    public ulong KernelTimestampMonotonic
    {
        get => _KernelTimestampMonotonic;
        set => _KernelTimestampMonotonic = value;
    }

    private ulong _InitRDTimestamp = default;

    public ulong InitRDTimestamp
    {
        get => _InitRDTimestamp;
        set => _InitRDTimestamp = value;
    }

    private ulong _InitRDTimestampMonotonic = default;

    public ulong InitRDTimestampMonotonic
    {
        get => _InitRDTimestampMonotonic;
        set => _InitRDTimestampMonotonic = value;
    }

    private ulong _UserspaceTimestamp = default;

    public ulong UserspaceTimestamp
    {
        get => _UserspaceTimestamp;
        set => _UserspaceTimestamp = value;
    }

    private ulong _UserspaceTimestampMonotonic = default;

    public ulong UserspaceTimestampMonotonic
    {
        get => _UserspaceTimestampMonotonic;
        set => _UserspaceTimestampMonotonic = value;
    }

    private ulong _FinishTimestamp = default;

    public ulong FinishTimestamp
    {
        get => _FinishTimestamp;
        set => _FinishTimestamp = value;
    }

    private ulong _FinishTimestampMonotonic = default;

    public ulong FinishTimestampMonotonic
    {
        get => _FinishTimestampMonotonic;
        set => _FinishTimestampMonotonic = value;
    }

    private ulong _SecurityStartTimestamp = default;

    public ulong SecurityStartTimestamp
    {
        get => _SecurityStartTimestamp;
        set => _SecurityStartTimestamp = value;
    }

    private ulong _SecurityStartTimestampMonotonic = default;

    public ulong SecurityStartTimestampMonotonic
    {
        get => _SecurityStartTimestampMonotonic;
        set => _SecurityStartTimestampMonotonic = value;
    }

    private ulong _SecurityFinishTimestamp = default;

    public ulong SecurityFinishTimestamp
    {
        get => _SecurityFinishTimestamp;
        set => _SecurityFinishTimestamp = value;
    }

    private ulong _SecurityFinishTimestampMonotonic = default;

    public ulong SecurityFinishTimestampMonotonic
    {
        get => _SecurityFinishTimestampMonotonic;
        set => _SecurityFinishTimestampMonotonic = value;
    }

    private ulong _GeneratorsStartTimestamp = default;

    public ulong GeneratorsStartTimestamp
    {
        get => _GeneratorsStartTimestamp;
        set => _GeneratorsStartTimestamp = value;
    }

    private ulong _GeneratorsStartTimestampMonotonic = default;

    public ulong GeneratorsStartTimestampMonotonic
    {
        get => _GeneratorsStartTimestampMonotonic;
        set => _GeneratorsStartTimestampMonotonic = value;
    }

    private ulong _GeneratorsFinishTimestamp = default;

    public ulong GeneratorsFinishTimestamp
    {
        get => _GeneratorsFinishTimestamp;
        set => _GeneratorsFinishTimestamp = value;
    }

    private ulong _GeneratorsFinishTimestampMonotonic = default;

    public ulong GeneratorsFinishTimestampMonotonic
    {
        get => _GeneratorsFinishTimestampMonotonic;
        set => _GeneratorsFinishTimestampMonotonic = value;
    }

    private ulong _UnitsLoadStartTimestamp = default;

    public ulong UnitsLoadStartTimestamp
    {
        get => _UnitsLoadStartTimestamp;
        set => _UnitsLoadStartTimestamp = value;
    }

    private ulong _UnitsLoadStartTimestampMonotonic = default;

    public ulong UnitsLoadStartTimestampMonotonic
    {
        get => _UnitsLoadStartTimestampMonotonic;
        set => _UnitsLoadStartTimestampMonotonic = value;
    }

    private ulong _UnitsLoadFinishTimestamp = default;

    public ulong UnitsLoadFinishTimestamp
    {
        get => _UnitsLoadFinishTimestamp;
        set => _UnitsLoadFinishTimestamp = value;
    }

    private ulong _UnitsLoadFinishTimestampMonotonic = default;

    public ulong UnitsLoadFinishTimestampMonotonic
    {
        get => _UnitsLoadFinishTimestampMonotonic;
        set => _UnitsLoadFinishTimestampMonotonic = value;
    }

    private ulong _UnitsLoadTimestamp = default;

    public ulong UnitsLoadTimestamp
    {
        get => _UnitsLoadTimestamp;
        set => _UnitsLoadTimestamp = value;
    }

    private ulong _UnitsLoadTimestampMonotonic = default;

    public ulong UnitsLoadTimestampMonotonic
    {
        get => _UnitsLoadTimestampMonotonic;
        set => _UnitsLoadTimestampMonotonic = value;
    }

    private ulong _InitRDSecurityStartTimestamp = default;

    public ulong InitRDSecurityStartTimestamp
    {
        get => _InitRDSecurityStartTimestamp;
        set => _InitRDSecurityStartTimestamp = value;
    }

    private ulong _InitRDSecurityStartTimestampMonotonic = default;

    public ulong InitRDSecurityStartTimestampMonotonic
    {
        get => _InitRDSecurityStartTimestampMonotonic;
        set => _InitRDSecurityStartTimestampMonotonic = value;
    }

    private ulong _InitRDSecurityFinishTimestamp = default;

    public ulong InitRDSecurityFinishTimestamp
    {
        get => _InitRDSecurityFinishTimestamp;
        set => _InitRDSecurityFinishTimestamp = value;
    }

    private ulong _InitRDSecurityFinishTimestampMonotonic = default;

    public ulong InitRDSecurityFinishTimestampMonotonic
    {
        get => _InitRDSecurityFinishTimestampMonotonic;
        set => _InitRDSecurityFinishTimestampMonotonic = value;
    }

    private ulong _InitRDGeneratorsStartTimestamp = default;

    public ulong InitRDGeneratorsStartTimestamp
    {
        get => _InitRDGeneratorsStartTimestamp;
        set => _InitRDGeneratorsStartTimestamp = value;
    }

    private ulong _InitRDGeneratorsStartTimestampMonotonic = default;

    public ulong InitRDGeneratorsStartTimestampMonotonic
    {
        get => _InitRDGeneratorsStartTimestampMonotonic;
        set => _InitRDGeneratorsStartTimestampMonotonic = value;
    }

    private ulong _InitRDGeneratorsFinishTimestamp = default;

    public ulong InitRDGeneratorsFinishTimestamp
    {
        get => _InitRDGeneratorsFinishTimestamp;
        set => _InitRDGeneratorsFinishTimestamp = value;
    }

    private ulong _InitRDGeneratorsFinishTimestampMonotonic = default;

    public ulong InitRDGeneratorsFinishTimestampMonotonic
    {
        get => _InitRDGeneratorsFinishTimestampMonotonic;
        set => _InitRDGeneratorsFinishTimestampMonotonic = value;
    }

    private ulong _InitRDUnitsLoadStartTimestamp = default;

    public ulong InitRDUnitsLoadStartTimestamp
    {
        get => _InitRDUnitsLoadStartTimestamp;
        set => _InitRDUnitsLoadStartTimestamp = value;
    }

    private ulong _InitRDUnitsLoadStartTimestampMonotonic = default;

    public ulong InitRDUnitsLoadStartTimestampMonotonic
    {
        get => _InitRDUnitsLoadStartTimestampMonotonic;
        set => _InitRDUnitsLoadStartTimestampMonotonic = value;
    }

    private ulong _InitRDUnitsLoadFinishTimestamp = default;

    public ulong InitRDUnitsLoadFinishTimestamp
    {
        get => _InitRDUnitsLoadFinishTimestamp;
        set => _InitRDUnitsLoadFinishTimestamp = value;
    }

    private ulong _InitRDUnitsLoadFinishTimestampMonotonic = default;

    public ulong InitRDUnitsLoadFinishTimestampMonotonic
    {
        get => _InitRDUnitsLoadFinishTimestampMonotonic;
        set => _InitRDUnitsLoadFinishTimestampMonotonic = value;
    }

    private string _LogLevel = string.Empty;

    public string LogLevel
    {
        get => _LogLevel;
        set => _LogLevel = value;
    }

    private string _LogTarget = string.Empty;

    public string LogTarget
    {
        get => _LogTarget;
        set => _LogTarget = value;
    }

    private uint _NNames = default;

    public uint NNames
    {
        get => _NNames;
        set => _NNames = value;
    }

    private uint _NFailedUnits = default;

    public uint NFailedUnits
    {
        get => _NFailedUnits;
        set => _NFailedUnits = value;
    }

    private uint _NJobs = default;

    public uint NJobs
    {
        get => _NJobs;
        set => _NJobs = value;
    }

    private uint _NInstalledJobs = default;

    public uint NInstalledJobs
    {
        get => _NInstalledJobs;
        set => _NInstalledJobs = value;
    }

    private uint _NFailedJobs = default;

    public uint NFailedJobs
    {
        get => _NFailedJobs;
        set => _NFailedJobs = value;
    }

    private double _Progress = default;

    public double Progress
    {
        get => _Progress;
        set => _Progress = value;
    }

    private string[] _Environment = [];
    public string[] Environment
    {
        get => _Environment;
        set => _Environment = value;
    }

    private bool _ConfirmSpawn = default;

    public bool ConfirmSpawn
    {
        get => _ConfirmSpawn;
        set => _ConfirmSpawn = value;
    }

    private bool _ShowStatus = default;

    public bool ShowStatus
    {
        get => _ShowStatus;
        set => _ShowStatus = value;
    }

    private string[] _UnitPath = [];

    public string[] UnitPath
    {
        get => _UnitPath;
        set => _UnitPath = value;
    }

    private string _DefaultStandardOutput = string.Empty;

    public string DefaultStandardOutput
    {
        get => _DefaultStandardOutput;
        set => _DefaultStandardOutput = value;
    }

    private string _DefaultStandardError = string.Empty;

    public string DefaultStandardError
    {
        get => _DefaultStandardError;
        set => _DefaultStandardError = value;
    }

    private string _WatchdogDevice = string.Empty;

    public string WatchdogDevice
    {
        get => _WatchdogDevice;
        set => _WatchdogDevice = value;
    }

    private ulong _WatchdogLastPingTimestamp = default;

    public ulong WatchdogLastPingTimestamp
    {
        get => _WatchdogLastPingTimestamp;
        set => _WatchdogLastPingTimestamp = value;
    }

    private ulong _WatchdogLastPingTimestampMonotonic = default;

    public ulong WatchdogLastPingTimestampMonotonic
    {
        get => _WatchdogLastPingTimestampMonotonic;
        set => _WatchdogLastPingTimestampMonotonic = value;
    }

    private ulong _RuntimeWatchdogUSec = default;

    public ulong RuntimeWatchdogUSec
    {
        get => _RuntimeWatchdogUSec;
        set => _RuntimeWatchdogUSec = value;
    }

    private ulong _RuntimeWatchdogPreUSec = default;

    public ulong RuntimeWatchdogPreUSec
    {
        get => _RuntimeWatchdogPreUSec;
        set => _RuntimeWatchdogPreUSec = value;
    }

    private string _RuntimeWatchdogPreGovernor = string.Empty;

    public string RuntimeWatchdogPreGovernor
    {
        get => _RuntimeWatchdogPreGovernor;
        set => _RuntimeWatchdogPreGovernor = value;
    }

    private ulong _RebootWatchdogUSec = default;

    public ulong RebootWatchdogUSec
    {
        get => _RebootWatchdogUSec;
        set => _RebootWatchdogUSec = value;
    }

    private ulong _KExecWatchdogUSec = default;

    public ulong KExecWatchdogUSec
    {
        get => _KExecWatchdogUSec;
        set => _KExecWatchdogUSec = value;
    }

    private bool _ServiceWatchdogs = default;

    public bool ServiceWatchdogs
    {
        get => _ServiceWatchdogs;
        set => _ServiceWatchdogs = value;
    }

    private string _ControlGroup = string.Empty;

    public string ControlGroup
    {
        get => _ControlGroup;
        set => _ControlGroup = value;
    }

    private string _SystemState = string.Empty;

    public string SystemState
    {
        get => _SystemState;
        set => _SystemState = value;
    }

    private byte _ExitCode = default;

    public byte ExitCode
    {
        get => _ExitCode;
        set => _ExitCode = value;
    }

    private ulong _DefaultTimerAccuracyUSec = default;

    public ulong DefaultTimerAccuracyUSec
    {
        get => _DefaultTimerAccuracyUSec;
        set => _DefaultTimerAccuracyUSec = value;
    }

    private ulong _DefaultTimeoutStartUSec = default;

    public ulong DefaultTimeoutStartUSec
    {
        get => _DefaultTimeoutStartUSec;
        set => _DefaultTimeoutStartUSec = value;
    }

    private ulong _DefaultTimeoutStopUSec = default;

    public ulong DefaultTimeoutStopUSec
    {
        get => _DefaultTimeoutStopUSec;
        set => _DefaultTimeoutStopUSec = value;
    }

    private ulong _DefaultTimeoutAbortUSec = default;

    public ulong DefaultTimeoutAbortUSec
    {
        get => _DefaultTimeoutAbortUSec;
        set => _DefaultTimeoutAbortUSec = value;
    }

    private ulong _DefaultDeviceTimeoutUSec = default;

    public ulong DefaultDeviceTimeoutUSec
    {
        get => _DefaultDeviceTimeoutUSec;
        set => _DefaultDeviceTimeoutUSec = value;
    }

    private ulong _DefaultRestartUSec = default;

    public ulong DefaultRestartUSec
    {
        get => _DefaultRestartUSec;
        set => _DefaultRestartUSec = value;
    }

    private ulong _DefaultStartLimitIntervalUSec = default;

    public ulong DefaultStartLimitIntervalUSec
    {
        get => _DefaultStartLimitIntervalUSec;
        set => _DefaultStartLimitIntervalUSec = value;
    }

    private uint _DefaultStartLimitBurst = default;

    public uint DefaultStartLimitBurst
    {
        get => _DefaultStartLimitBurst;
        set => _DefaultStartLimitBurst = value;
    }

    private bool _DefaultCPUAccounting = default;

    public bool DefaultCPUAccounting
    {
        get => _DefaultCPUAccounting;
        set => _DefaultCPUAccounting = value;
    }

    private bool _DefaultBlockIOAccounting = default;

    public bool DefaultBlockIOAccounting
    {
        get => _DefaultBlockIOAccounting;
        set => _DefaultBlockIOAccounting = value;
    }

    private bool _DefaultIOAccounting = default;

    public bool DefaultIOAccounting
    {
        get => _DefaultIOAccounting;
        set => _DefaultIOAccounting = value;
    }

    private bool _DefaultIPAccounting = default;

    public bool DefaultIPAccounting
    {
        get => _DefaultIPAccounting;
        set => _DefaultIPAccounting = value;
    }

    private bool _DefaultMemoryAccounting = default;

    public bool DefaultMemoryAccounting
    {
        get => _DefaultMemoryAccounting;
        set => _DefaultMemoryAccounting = value;
    }

    private bool _DefaultTasksAccounting = default;

    public bool DefaultTasksAccounting
    {
        get => _DefaultTasksAccounting;
        set => _DefaultTasksAccounting = value;
    }

    private ulong _DefaultLimitCPU = default;

    public ulong DefaultLimitCPU
    {
        get => _DefaultLimitCPU;
        set => _DefaultLimitCPU = value;
    }

    private ulong _DefaultLimitCPUSoft = default;

    public ulong DefaultLimitCPUSoft
    {
        get => _DefaultLimitCPUSoft;
        set => _DefaultLimitCPUSoft = value;
    }

    private ulong _DefaultLimitFSIZE = default;

    public ulong DefaultLimitFSIZE
    {
        get => _DefaultLimitFSIZE;
        set => _DefaultLimitFSIZE = value;
    }

    private ulong _DefaultLimitFSIZESoft = default;

    public ulong DefaultLimitFSIZESoft
    {
        get => _DefaultLimitFSIZESoft;
        set => _DefaultLimitFSIZESoft = value;
    }

    private ulong _DefaultLimitDATA = default;

    public ulong DefaultLimitDATA
    {
        get => _DefaultLimitDATA;
        set => _DefaultLimitDATA = value;
    }

    private ulong _DefaultLimitDATASoft = default;

    public ulong DefaultLimitDATASoft
    {
        get => _DefaultLimitDATASoft;
        set => _DefaultLimitDATASoft = value;
    }

    private ulong _DefaultLimitSTACK = default;

    public ulong DefaultLimitSTACK
    {
        get => _DefaultLimitSTACK;
        set => _DefaultLimitSTACK = value;
    }

    private ulong _DefaultLimitSTACKSoft = default;

    public ulong DefaultLimitSTACKSoft
    {
        get => _DefaultLimitSTACKSoft;
        set => _DefaultLimitSTACKSoft = value;
    }

    private ulong _DefaultLimitCORE = default;

    public ulong DefaultLimitCORE
    {
        get => _DefaultLimitCORE;
        set => _DefaultLimitCORE = value;
    }

    private ulong _DefaultLimitCORESoft = default;

    public ulong DefaultLimitCORESoft
    {
        get => _DefaultLimitCORESoft;
        set => _DefaultLimitCORESoft = value;
    }

    private ulong _DefaultLimitRSS = default;

    public ulong DefaultLimitRSS
    {
        get => _DefaultLimitRSS;
        set => _DefaultLimitRSS = value;
    }

    private ulong _DefaultLimitRSSSoft = default;

    public ulong DefaultLimitRSSSoft
    {
        get => _DefaultLimitRSSSoft;
        set => _DefaultLimitRSSSoft = value;
    }

    private ulong _DefaultLimitNOFILE = default;

    public ulong DefaultLimitNOFILE
    {
        get => _DefaultLimitNOFILE;
        set => _DefaultLimitNOFILE = value;
    }

    private ulong _DefaultLimitNOFILESoft = default;

    public ulong DefaultLimitNOFILESoft
    {
        get => _DefaultLimitNOFILESoft;
        set => _DefaultLimitNOFILESoft = value;
    }

    private ulong _DefaultLimitAS = default;

    public ulong DefaultLimitAS
    {
        get => _DefaultLimitAS;
        set => _DefaultLimitAS = value;
    }

    private ulong _DefaultLimitASSoft = default;

    public ulong DefaultLimitASSoft
    {
        get => _DefaultLimitASSoft;
        set => _DefaultLimitASSoft = value;
    }

    private ulong _DefaultLimitNPROC = default;

    public ulong DefaultLimitNPROC
    {
        get => _DefaultLimitNPROC;
        set => _DefaultLimitNPROC = value;
    }

    private ulong _DefaultLimitNPROCSoft = default;

    public ulong DefaultLimitNPROCSoft
    {
        get => _DefaultLimitNPROCSoft;
        set => _DefaultLimitNPROCSoft = value;
    }

    private ulong _DefaultLimitMEMLOCK = default;

    public ulong DefaultLimitMEMLOCK
    {
        get => _DefaultLimitMEMLOCK;
        set => _DefaultLimitMEMLOCK = value;
    }

    private ulong _DefaultLimitMEMLOCKSoft = default;

    public ulong DefaultLimitMEMLOCKSoft
    {
        get => _DefaultLimitMEMLOCKSoft;
        set => _DefaultLimitMEMLOCKSoft = value;
    }

    private ulong _DefaultLimitLOCKS = default;

    public ulong DefaultLimitLOCKS
    {
        get => _DefaultLimitLOCKS;
        set => _DefaultLimitLOCKS = value;
    }

    private ulong _DefaultLimitLOCKSSoft = default;

    public ulong DefaultLimitLOCKSSoft
    {
        get => _DefaultLimitLOCKSSoft;
        set => _DefaultLimitLOCKSSoft = value;
    }

    private ulong _DefaultLimitSIGPENDING = default;

    public ulong DefaultLimitSIGPENDING
    {
        get => _DefaultLimitSIGPENDING;
        set => _DefaultLimitSIGPENDING = value;
    }

    private ulong _DefaultLimitSIGPENDINGSoft = default;

    public ulong DefaultLimitSIGPENDINGSoft
    {
        get => _DefaultLimitSIGPENDINGSoft;
        set => _DefaultLimitSIGPENDINGSoft = value;
    }

    private ulong _DefaultLimitMSGQUEUE = default;

    public ulong DefaultLimitMSGQUEUE
    {
        get => _DefaultLimitMSGQUEUE;
        set => _DefaultLimitMSGQUEUE = value;
    }

    private ulong _DefaultLimitMSGQUEUESoft = default;

    public ulong DefaultLimitMSGQUEUESoft
    {
        get => _DefaultLimitMSGQUEUESoft;
        set => _DefaultLimitMSGQUEUESoft = value;
    }

    private ulong _DefaultLimitNICE = default;

    public ulong DefaultLimitNICE
    {
        get => _DefaultLimitNICE;
        set => _DefaultLimitNICE = value;
    }

    private ulong _DefaultLimitNICESoft = default;

    public ulong DefaultLimitNICESoft
    {
        get => _DefaultLimitNICESoft;
        set => _DefaultLimitNICESoft = value;
    }

    private ulong _DefaultLimitRTPRIO = default;

    public ulong DefaultLimitRTPRIO
    {
        get => _DefaultLimitRTPRIO;
        set => _DefaultLimitRTPRIO = value;
    }

    private ulong _DefaultLimitRTPRIOSoft = default;

    public ulong DefaultLimitRTPRIOSoft
    {
        get => _DefaultLimitRTPRIOSoft;
        set => _DefaultLimitRTPRIOSoft = value;
    }

    private ulong _DefaultLimitRTTIME = default;

    public ulong DefaultLimitRTTIME
    {
        get => _DefaultLimitRTTIME;
        set => _DefaultLimitRTTIME = value;
    }

    private ulong _DefaultLimitRTTIMESoft = default;

    public ulong DefaultLimitRTTIMESoft
    {
        get => _DefaultLimitRTTIMESoft;
        set => _DefaultLimitRTTIMESoft = value;
    }

    private ulong _DefaultTasksMax = default;

    public ulong DefaultTasksMax
    {
        get => _DefaultTasksMax;
        set => _DefaultTasksMax = value;
    }

    private ulong _DefaultMemoryPressureThresholdUSec = default;

    public ulong DefaultMemoryPressureThresholdUSec
    {
        get => _DefaultMemoryPressureThresholdUSec;
        set => _DefaultMemoryPressureThresholdUSec = value;
    }

    private string _DefaultMemoryPressureWatch = string.Empty;

    public string DefaultMemoryPressureWatch
    {
        get => _DefaultMemoryPressureWatch;
        set => _DefaultMemoryPressureWatch = value;
    }

    private ulong _TimerSlackNSec = default;

    public ulong TimerSlackNSec
    {
        get => _TimerSlackNSec;
        set => _TimerSlackNSec = value;
    }

    private string _DefaultOOMPolicy = string.Empty;

    public string DefaultOOMPolicy
    {
        get => _DefaultOOMPolicy;
        set => _DefaultOOMPolicy = value;
    }

    private int _DefaultOOMScoreAdjust = default;

    public int DefaultOOMScoreAdjust
    {
        get => _DefaultOOMScoreAdjust;
        set => _DefaultOOMScoreAdjust = value;
    }

    private string _CtrlAltDelBurstAction = string.Empty;

    public string CtrlAltDelBurstAction
    {
        get => _CtrlAltDelBurstAction;
        set => _CtrlAltDelBurstAction = value;
    }
}
