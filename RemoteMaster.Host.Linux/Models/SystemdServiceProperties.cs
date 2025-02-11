// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using Tmds.DBus;

namespace RemoteMaster.Host.Linux.Models;

[Dictionary]
public class SystemdServiceProperties
{
    public string Type { get; set; } = string.Empty;

    public string ExitType { get; set; } = string.Empty;

    public string Restart { get; set; } = string.Empty;

    public string RestartMode { get; set; } = string.Empty;

    public string PIDFile { get; set; } = string.Empty;

    public string NotifyAccess { get; set; } = string.Empty;

    public ulong RestartUSec { get; set; }

    public uint RestartSteps { get; set; }

    public ulong RestartMaxDelayUSec { get; set; }

    public ulong RestartUSecNext { get; set; }

    public ulong TimeoutStartUSec { get; set; }

    public ulong TimeoutStopUSec { get; set; }

    public ulong TimeoutAbortUSec { get; set; }

    public string TimeoutStartFailureMode { get; set; } = string.Empty;

    public string TimeoutStopFailureMode { get; set; } = string.Empty;

    public ulong RuntimeMaxUSec { get; set; }

    public ulong RuntimeRandomizedExtraUSec { get; set; }

    public ulong WatchdogUSec { get; set; }

    public ulong WatchdogTimestamp { get; set; }

    public ulong WatchdogTimestampMonotonic { get; set; }

    public bool RootDirectoryStartOnly { get; set; }

    public bool RemainAfterExit { get; set; }

    public bool GuessMainPID { get; set; }

    public (int[], int[]) RestartPreventExitStatus { get; set; }

    public (int[], int[]) RestartForceExitStatus { get; set; }

    public (int[], int[]) SuccessExitStatus { get; set; }

    public uint MainPID { get; set; }

    public uint ControlPID { get; set; }

    public string BusName { get; set; } = string.Empty;

    public uint FileDescriptorStoreMax { get; set; }

    public uint NFileDescriptorStore { get; set; }

    public string FileDescriptorStorePreserve { get; set; } = string.Empty;

    public string StatusText { get; set; } = string.Empty;

    public int StatusErrno { get; set; }

    public string Result { get; set; } = string.Empty;

    public string ReloadResult { get; set; } = string.Empty;

    public string CleanResult { get; set; } = string.Empty;

    public string USBFunctionDescriptors { get; set; } = string.Empty;

    public string USBFunctionStrings { get; set; } = string.Empty;

    public uint UID { get; set; }

    public uint GID { get; set; }

    public uint NRestarts { get; set; }

    public string OOMPolicy { get; set; } = string.Empty;

    public (string, string, ulong)[] OpenFile { get; set; } = [];

    public int ReloadSignal { get; set; }

    public ulong ExecMainStartTimestamp { get; set; }

    public ulong ExecMainStartTimestampMonotonic { get; set; }

    public ulong ExecMainExitTimestamp { get; set; }

    public ulong ExecMainExitTimestampMonotonic { get; set; }

    public uint ExecMainPID { get; set; }

    public int ExecMainCode { get; set; }

    public int ExecMainStatus { get; set; }

    public (string, string[], bool, ulong, ulong, ulong, ulong, uint, int, int)[] ExecCondition { get; set; } = [];

    public (string, string[], string[], ulong, ulong, ulong, ulong, uint, int, int)[] ExecConditionEx { get; set; } = [];

    public (string, string[], bool, ulong, ulong, ulong, ulong, uint, int, int)[] ExecStartPre { get; set; } = [];

    public (string, string[], string[], ulong, ulong, ulong, ulong, uint, int, int)[] ExecStartPreEx { get; set; } = [];

    public (string, string[], bool, ulong, ulong, ulong, ulong, uint, int, int)[] ExecStart { get; set; } = [];

    public (string, string[], string[], ulong, ulong, ulong, ulong, uint, int, int)[] ExecStartEx { get; set; } = [];

    public (string, string[], bool, ulong, ulong, ulong, ulong, uint, int, int)[] ExecStartPost { get; set; } = [];

    public (string, string[], string[], ulong, ulong, ulong, ulong, uint, int, int)[] ExecStartPostEx { get; set; } = [];

    public (string, string[], bool, ulong, ulong, ulong, ulong, uint, int, int)[] ExecReload { get; set; } = [];

    public (string, string[], string[], ulong, ulong, ulong, ulong, uint, int, int)[] ExecReloadEx { get; set; } = [];

    public (string, string[], bool, ulong, ulong, ulong, ulong, uint, int, int)[] ExecStop { get; set; } = [];

    public (string, string[], string[], ulong, ulong, ulong, ulong, uint, int, int)[] ExecStopEx { get; set; } = [];

    public (string, string[], bool, ulong, ulong, ulong, ulong, uint, int, int)[] ExecStopPost { get; set; } = [];

    public (string, string[], string[], ulong, ulong, ulong, ulong, uint, int, int)[] ExecStopPostEx { get; set; } = [];

    public string Slice { get; set; } = string.Empty;

    public string ControlGroup { get; set; } = string.Empty;

    public ulong ControlGroupId { get; set; }

    public ulong MemoryCurrent { get; set; }

    public ulong MemoryPeak { get; set; }

    public ulong MemorySwapCurrent { get; set; }

    public ulong MemorySwapPeak { get; set; }

    public ulong MemoryZSwapCurrent { get; set; }

    public ulong MemoryAvailable { get; set; }

    public ulong CPUUsageNSec { get; set; }

    public byte[] EffectiveCPUs { get; set; } = [];

    public byte[] EffectiveMemoryNodes { get; set; } = [];

    public ulong TasksCurrent { get; set; }

    public ulong IPIngressBytes { get; set; }

    public ulong IPIngressPackets { get; set; }

    public ulong IPEgressBytes { get; set; }

    public ulong IPEgressPackets { get; set; }

    public ulong IOReadBytes { get; set; }

    public ulong IOReadOperations { get; set; }

    public ulong IOWriteBytes { get; set; }

    public ulong IOWriteOperations { get; set; }

    public bool Delegate { get; set; }

    public string[] DelegateControllers { get; set; } = [];

    public string DelegateSubgroup { get; set; } = string.Empty;

    public bool CPUAccounting { get; set; }

    public ulong CPUWeight { get; set; }

    public ulong StartupCPUWeight { get; set; }

    public ulong CPUShares { get; set; }

    public ulong StartupCPUShares { get; set; }

    public ulong CPUQuotaPerSecUSec { get; set; }

    public ulong CPUQuotaPeriodUSec { get; set; }

    public byte[] AllowedCPUs { get; set; } = [];

    public byte[] StartupAllowedCPUs { get; set; } = [];

    public byte[] AllowedMemoryNodes { get; set; } = [];

    public byte[] StartupAllowedMemoryNodes { get; set; } = [];

    public bool IOAccounting { get; set; }

    public ulong IOWeight { get; set; }

    public ulong StartupIOWeight { get; set; }

    public (string, ulong)[] IODeviceWeight { get; set; } = [];

    public (string, ulong)[] IOReadBandwidthMax { get; set; } = [];

    public (string, ulong)[] IOWriteBandwidthMax { get; set; } = [];

    public (string, ulong)[] IOReadIOPSMax { get; set; } = [];

    public (string, ulong)[] IOWriteIOPSMax { get; set; } = [];

    public (string, ulong)[] IODeviceLatencyTargetUSec { get; set; } = [];

    public bool BlockIOAccounting { get; set; }

    public ulong BlockIOWeight { get; set; }

    public ulong StartupBlockIOWeight { get; set; }

    public (string, ulong)[] BlockIODeviceWeight { get; set; } = [];

    public (string, ulong)[] BlockIOReadBandwidth { get; set; } = [];

    public (string, ulong)[] BlockIOWriteBandwidth { get; set; } = [];

    public bool MemoryAccounting { get; set; }

    public ulong DefaultMemoryLow { get; set; }

    public ulong DefaultStartupMemoryLow { get; set; }

    public ulong DefaultMemoryMin { get; set; }

    public ulong MemoryMin { get; set; }

    public ulong MemoryLow { get; set; }

    public ulong StartupMemoryLow { get; set; }

    public ulong MemoryHigh { get; set; }

    public ulong StartupMemoryHigh { get; set; }

    public ulong MemoryMax { get; set; }

    public ulong StartupMemoryMax { get; set; }

    public ulong MemorySwapMax { get; set; }

    public ulong StartupMemorySwapMax { get; set; }

    public ulong MemoryZSwapMax { get; set; }

    public ulong StartupMemoryZSwapMax { get; set; }

    public ulong MemoryLimit { get; set; }

    public string DevicePolicy { get; set; } = string.Empty;

    public (string, string)[] DeviceAllow { get; set; } = [];

    public bool TasksAccounting { get; set; }

    public ulong TasksMax { get; set; }

    public bool IPAccounting { get; set; }

    public (int, byte[], uint)[] IPAddressAllow { get; set; } = [];

    public (int, byte[], uint)[] IPAddressDeny { get; set; } = [];

    public string[] IPIngressFilterPath { get; set; } = [];

    public string[] IPEgressFilterPath { get; set; } = [];

    public string[] DisableControllers { get; set; } = [];

    public string ManagedOOMSwap { get; set; } = string.Empty;

    public string ManagedOOMMemoryPressure { get; set; } = string.Empty;

    public uint ManagedOOMMemoryPressureLimit { get; set; }

    public string ManagedOOMPreference { get; set; } = string.Empty;

    public (string, string)[] BPFProgram { get; set; } = [];

    public (int, int, ushort, ushort)[] SocketBindAllow { get; set; } = [];

    public (int, int, ushort, ushort)[] SocketBindDeny { get; set; } = [];

    public (bool, string[]) RestrictNetworkInterfaces { get; set; }

    public string MemoryPressureWatch { get; set; } = string.Empty;

    public ulong MemoryPressureThresholdUSec { get; set; }

    public (int, int, string, string)[] NFTSet { get; set; } = [];

    public bool CoredumpReceive { get; set; }

    public string[] Environment { get; set; } = [];

    public (string, bool)[] EnvironmentFiles { get; set; } = [];

    public string[] PassEnvironment { get; set; } = [];

    public string[] UnsetEnvironment { get; set; } = [];

    public uint UMask { get; set; }

    public ulong LimitCPU { get; set; }

    public ulong LimitCPUSoft { get; set; }

    public ulong LimitFSIZE { get; set; }

    public ulong LimitFSIZESoft { get; set; }

    public ulong LimitDATA { get; set; }

    public ulong LimitDATASoft { get; set; }

    public ulong LimitSTACK { get; set; }

    public ulong LimitSTACKSoft { get; set; }

    public ulong LimitCORE { get; set; }

    public ulong LimitCORESoft { get; set; }

    public ulong LimitRSS { get; set; }

    public ulong LimitRSSSoft { get; set; }

    public ulong LimitNOFILE { get; set; }

    public ulong LimitNOFILESoft { get; set; }

    public ulong LimitAS { get; set; }

    public ulong LimitASSoft { get; set; }

    public ulong LimitNPROC { get; set; }

    public ulong LimitNPROCSoft { get; set; }

    public ulong LimitMEMLOCK { get; set; }

    public ulong LimitMEMLOCKSoft { get; set; }

    public ulong LimitLOCKS { get; set; }

    public ulong LimitLOCKSSoft { get; set; }

    public ulong LimitSIGPENDING { get; set; }

    public ulong LimitSIGPENDINGSoft { get; set; }

    public ulong LimitMSGQUEUE { get; set; }

    public ulong LimitMSGQUEUESoft { get; set; }

    public ulong LimitNICE { get; set; }

    public ulong LimitNICESoft { get; set; }

    public ulong LimitRTPRIO { get; set; }

    public ulong LimitRTPRIOSoft { get; set; }

    public ulong LimitRTTIME { get; set; }

    public ulong LimitRTTIMESoft { get; set; }

    public string WorkingDirectory { get; set; } = string.Empty;

    public string RootDirectory { get; set; } = string.Empty;

    public string RootImage { get; set; } = string.Empty;

    public (string, string)[] RootImageOptions { get; set; } = [];

    public byte[] RootHash { get; set; } = [];

    public string RootHashPath { get; set; } = string.Empty;

    public byte[] RootHashSignature { get; set; } = [];

    public string RootHashSignaturePath { get; set; } = string.Empty;

    public string RootVerity { get; set; } = string.Empty;

    public bool RootEphemeral { get; set; }

    public string[] ExtensionDirectories { get; set; } = [];

    public (string, bool, (string, string)[])[] ExtensionImages { get; set; } = [];

    public (string, string, bool, (string, string)[])[] MountImages { get; set; } = [];

    public int OOMScoreAdjust { get; set; }

    public ulong CoredumpFilter { get; set; }

    public int Nice { get; set; }

    public int IOSchedulingClass { get; set; }

    public int IOSchedulingPriority { get; set; }

    public int CPUSchedulingPolicy { get; set; }

    public int CPUSchedulingPriority { get; set; }

    public byte[] CPUAffinity { get; set; } = [];

    public bool CPUAffinityFromNUMA { get; set; }

    public int NUMAPolicy { get; set; }

    public byte[] NUMAMask { get; set; } = [];

    public ulong TimerSlackNSec { get; set; }

    public bool CPUSchedulingResetOnFork { get; set; }

    public bool NonBlocking { get; set; }

    public string StandardInput { get; set; } = string.Empty;

    public string StandardInputFileDescriptorName { get; set; } = string.Empty;

    public byte[] StandardInputData { get; set; } = [];

    public string StandardOutput { get; set; } = string.Empty;

    public string StandardOutputFileDescriptorName { get; set; } = string.Empty;

    public string StandardError { get; set; } = string.Empty;

    public string StandardErrorFileDescriptorName { get; set; } = string.Empty;

    public string TTYPath { get; set; } = string.Empty;

    public bool TTYReset { get; set; }

    public bool TTYVHangup { get; set; }

    public bool TTYVTDisallocate { get; set; }

    public ushort TTYRows { get; set; }

    public ushort TTYColumns { get; set; }

    public int SyslogPriority { get; set; }

    public string SyslogIdentifier { get; set; } = string.Empty;

    public bool SyslogLevelPrefix { get; set; }

    public int SyslogLevel { get; set; }

    public int SyslogFacility { get; set; }

    public int LogLevelMax { get; set; }

    public ulong LogRateLimitIntervalUSec { get; set; }

    public uint LogRateLimitBurst { get; set; }

    public byte[][] LogExtraFields { get; set; } = [];

    public (bool, string)[] LogFilterPatterns { get; set; } = [];

    public string LogNamespace { get; set; } = string.Empty;

    public int SecureBits { get; set; }

    public ulong CapabilityBoundingSet { get; set; }

    public ulong AmbientCapabilities { get; set; }

    public string User { get; set; } = string.Empty;

    public string Group { get; set; } = string.Empty;

    public bool DynamicUser { get; set; }

    public bool SetLoginEnvironment { get; set; }

    public bool RemoveIPC { get; set; }

    public (string, byte[])[] SetCredential { get; set; } = [];

    public (string, byte[])[] SetCredentialEncrypted { get; set; } = [];

    public (string, string)[] LoadCredential { get; set; } = [];

    public (string, string)[] LoadCredentialEncrypted { get; set; } = [];

    public string[] ImportCredential { get; set; } = [];

    public string[] SupplementaryGroups { get; set; } = [];

    public string PAMName { get; set; } = string.Empty;

    public string[] ReadWritePaths { get; set; } = [];

    public string[] ReadOnlyPaths { get; set; } = [];

    public string[] InaccessiblePaths { get; set; } = [];

    public string[] ExecPaths { get; set; } = [];

    public string[] NoExecPaths { get; set; } = [];

    public string[] ExecSearchPath { get; set; } = [];

    public ulong MountFlags { get; set; }

    public bool PrivateTmp { get; set; }

    public bool PrivateDevices { get; set; }

    public bool ProtectClock { get; set; }

    public bool ProtectKernelTunables { get; set; }

    public bool ProtectKernelModules { get; set; }

    public bool ProtectKernelLogs { get; set; }

    public bool ProtectControlGroups { get; set; }

    public bool PrivateNetwork { get; set; }

    public bool PrivateUsers { get; set; }

    public bool PrivateMounts { get; set; }

    public bool PrivateIPC { get; set; }

    public string ProtectHome { get; set; } = string.Empty;

    public string ProtectSystem { get; set; } = string.Empty;

    public bool SameProcessGroup { get; set; }

    public string UtmpIdentifier { get; set; } = string.Empty;

    public string UtmpMode { get; set; } = string.Empty;

    public (bool, string) SELinuxContext { get; set; }

    public (bool, string) AppArmorProfile { get; set; }

    public (bool, string) SmackProcessLabel { get; set; }

    public bool IgnoreSIGPIPE { get; set; }

    public bool NoNewPrivileges { get; set; }

    public (bool, string[]) SystemCallFilter { get; set; }

    public string[] SystemCallArchitectures { get; set; } = [];

    public int SystemCallErrorNumber { get; set; }

    public (bool, string[]) SystemCallLog { get; set; }

    public string Personality { get; set; } = string.Empty;

    public bool LockPersonality { get; set; }

    public (bool, string[]) RestrictAddressFamilies { get; set; }

    public (string, string, ulong)[] RuntimeDirectorySymlink { get; set; } = [];

    public string RuntimeDirectoryPreserve { get; set; } = string.Empty;

    public uint RuntimeDirectoryMode { get; set; }

    public string[] RuntimeDirectory { get; set; } = [];

    public (string, string, ulong)[] StateDirectorySymlink { get; set; } = [];

    public uint StateDirectoryMode { get; set; }

    public string[] StateDirectory { get; set; } = [];

    public (string, string, ulong)[] CacheDirectorySymlink { get; set; } = [];

    public uint CacheDirectoryMode { get; set; }

    public string[] CacheDirectory { get; set; } = [];

    public (string, string, ulong)[] LogsDirectorySymlink { get; set; } = [];

    public uint LogsDirectoryMode { get; set; }

    public string[] LogsDirectory { get; set; } = [];

    public uint ConfigurationDirectoryMode { get; set; }

    public string[] ConfigurationDirectory { get; set; } = [];

    public ulong TimeoutCleanUSec { get; set; }

    public bool MemoryDenyWriteExecute { get; set; }

    public bool RestrictRealtime { get; set; }

    public bool RestrictSUIDSGID { get; set; }

    public ulong RestrictNamespaces { get; set; }

    public (bool, string[]) RestrictFileSystems { get; set; }

    public (string, string, bool, ulong)[] BindPaths { get; set; } = [];

    public (string, string, bool, ulong)[] BindReadOnlyPaths { get; set; } = [];

    public (string, string)[] TemporaryFileSystem { get; set; } = [];

    public bool MountAPIVFS { get; set; }

    public string KeyringMode { get; set; } = string.Empty;

    public string ProtectProc { get; set; } = string.Empty;

    public string ProcSubset { get; set; } = string.Empty;

    public bool ProtectHostname { get; set; }

    public bool MemoryKSM { get; set; }

    public string NetworkNamespacePath { get; set; } = string.Empty;

    public string IPCNamespacePath { get; set; } = string.Empty;

    public string RootImagePolicy { get; set; } = string.Empty;

    public string MountImagePolicy { get; set; } = string.Empty;

    public string ExtensionImagePolicy { get; set; } = string.Empty;

    public string KillMode { get; set; } = string.Empty;

    public int KillSignal { get; set; }

    public int RestartKillSignal { get; set; }

    public int FinalKillSignal { get; set; }

    public bool SendSIGKILL { get; set; }

    public bool SendSIGHUP { get; set; }

    public int WatchdogSignal { get; set; }
}
