// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using Tmds.DBus;

namespace RemoteMaster.Host.Linux.Models;

[Dictionary]
public class SystemdUnitProperties
{
    public string Id { get; set; } = string.Empty;

    public string[] Names { get; set; } = [];

    public string Following { get; set; } = string.Empty;

    public string[] Requires { get; set; } = [];

    public string[] Requisite { get; set; } = [];

    public string[] Wants { get; set; } = [];

    public string[] BindsTo { get; set; } = [];

    public string[] PartOf { get; set; } = [];

    public string[] Upholds { get; set; } = [];

    public string[] RequiredBy { get; set; } = [];

    public string[] RequisiteOf { get; set; } = [];

    public string[] WantedBy { get; set; } = [];

    public string[] BoundBy { get; set; } = [];

    public string[] UpheldBy { get; set; } = [];

    public string[] ConsistsOf { get; set; } = [];

    public string[] Conflicts { get; set; } = [];

    public string[] ConflictedBy { get; set; } = [];

    public string[] Before { get; set; } = [];

    public string[] After { get; set; } = [];

    public string[] OnSuccess { get; set; } = [];

    public string[] OnSuccessOf { get; set; } = [];

    public string[] OnFailure { get; set; } = [];

    public string[] OnFailureOf { get; set; } = [];

    public string[] Triggers { get; set; } = [];

    public string[] TriggeredBy { get; set; } = [];

    public string[] PropagatesReloadTo { get; set; } = [];

    public string[] ReloadPropagatedFrom { get; set; } = [];

    public string[] PropagatesStopTo { get; set; } = [];

    public string[] StopPropagatedFrom { get; set; } = [];

    public string[] JoinsNamespaceOf { get; set; } = [];

    public string[] SliceOf { get; set; } = [];

    public string[] RequiresMountsFor { get; set; } = [];

    public string[] Documentation { get; set; } = [];

    public string Description { get; set; } = string.Empty;

    public string AccessSELinuxContext { get; set; } = string.Empty;

    public string LoadState { get; set; } = string.Empty;

    public string ActiveState { get; set; } = string.Empty;

    public string FreezerState { get; set; } = string.Empty;

    public string SubState { get; set; } = string.Empty;

    public string FragmentPath { get; set; } = string.Empty;

    public string SourcePath { get; set; } = string.Empty;

    public string[] DropInPaths { get; set; } = [];

    public string UnitFileState { get; set; } = string.Empty;

    public string UnitFilePreset { get; set; } = string.Empty;

    public ulong StateChangeTimestamp { get; set; }

    public ulong StateChangeTimestampMonotonic { get; set; }

    public ulong InactiveExitTimestamp { get; set; }

    public ulong InactiveExitTimestampMonotonic { get; set; }

    public ulong ActiveEnterTimestamp { get; set; }

    public ulong ActiveEnterTimestampMonotonic { get; set; }

    public ulong ActiveExitTimestamp { get; set; }

    public ulong ActiveExitTimestampMonotonic { get; set; }

    public ulong InactiveEnterTimestamp { get; set; }

    public ulong InactiveEnterTimestampMonotonic { get; set; }

    public bool CanStart { get; set; }

    public bool CanStop { get; set; }

    public bool CanReload { get; set; }

    public bool CanIsolate { get; set; }

    public string[] CanClean { get; set; } = [];

    public bool CanFreeze { get; set; }

    public (uint, ObjectPath) Job { get; set; }

    public bool StopWhenUnneeded { get; set; }

    public bool RefuseManualStart { get; set; }

    public bool RefuseManualStop { get; set; }

    public bool AllowIsolate { get; set; }

    public bool DefaultDependencies { get; set; }

    public bool SurviveFinalKillSignal { get; set; }

    public string OnSuccessJobMode { get; set; } = string.Empty;

    public string OnFailureJobMode { get; set; } = string.Empty;

    public bool IgnoreOnIsolate { get; set; }

    public bool NeedDaemonReload { get; set; }

    public string[] Markers { get; set; } = [];

    public ulong JobTimeoutUSec { get; set; }

    public ulong JobRunningTimeoutUSec { get; set; }

    public string JobTimeoutAction { get; set; } = string.Empty;

    public string JobTimeoutRebootArgument { get; set; } = string.Empty;

    public bool ConditionResult { get; set; }

    public bool AssertResult { get; set; }

    public ulong ConditionTimestamp { get; set; }

    public ulong ConditionTimestampMonotonic { get; set; }

    public ulong AssertTimestamp { get; set; }

    public ulong AssertTimestampMonotonic { get; set; }

    public (string, bool, bool, string, int)[] Conditions { get; set; } = [];

    public (string, bool, bool, string, int)[] Asserts { get; set; } = [];

    public (string, string) LoadError { get; set; }

    public bool Transient { get; set; }

    public bool Perpetual { get; set; }

    public ulong StartLimitIntervalUSec { get; set; }

    public uint StartLimitBurst { get; set; }

    public string StartLimitAction { get; set; } = string.Empty;

    public string FailureAction { get; set; } = string.Empty;

    public int FailureActionExitStatus { get; set; }

    public string SuccessAction { get; set; } = string.Empty;

    public int SuccessActionExitStatus { get; set; }

    public string RebootArgument { get; set; } = string.Empty;

    public byte[] InvocationID { get; set; } = [];

    public string CollectMode { get; set; } = string.Empty;

    public string[] Refs { get; set; } = [];

    public (string, string)[] ActivationDetails { get; set; } = [];
}
