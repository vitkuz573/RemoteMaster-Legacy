// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Diagnostics;
using System.ServiceProcess;
using RemoteMaster.Host.Core.Abstractions;

namespace RemoteMaster.Host.Windows.Abstractions;

public abstract class AbstractService(IProcessWrapperFactory processWrapperFactory) : IService
{
    public abstract string Name { get; }

    protected abstract string DisplayName { get; }

    protected abstract string BinPath { get; }

    protected abstract IDictionary<string, string?> Arguments { get; }

    protected abstract string? Description { get; }

    protected abstract ServiceStartType StartType { get; }

    protected abstract IEnumerable<string>? Dependencies { get; }

    protected abstract int ResetPeriod { get; }

    protected abstract FailureAction FirstFailureAction { get; }

    protected abstract FailureAction SecondFailureAction { get; }

    protected abstract FailureAction SubsequentFailuresAction { get; }

    protected abstract string? RebootMessage { get; }

    protected abstract string? RestartCommand { get; }

    public virtual bool IsInstalled => ServiceController.GetServices().Any(service => service.ServiceName == Name);

    public virtual bool IsRunning
    {
        get
        {
            if (!IsInstalled)
            {
                return false;
            }

            try
            {
                using var serviceController = new ServiceController(Name);

                return serviceController.Status == ServiceControllerStatus.Running;
            }
            catch
            {
                return false;
            }
        }
    }

    public virtual void Create()
    {
        var binPath = $"{BinPath} {string.Join(" ", Arguments.Select(kv => kv.Value == null ? $"{kv.Key}" : $"{kv.Key}={kv.Value}"))}";

        ExecuteServiceCommand($"create {Name} DisplayName= \"{DisplayName}\" binPath= \"{binPath}\" start= {StartType}");

        if (!string.IsNullOrWhiteSpace(Description))
        {
            ExecuteServiceCommand($"description {Name} \"{Description}\"");
        }

        var failureActions = $"failure \"{Name}\" reset= {ResetPeriod} " +
                             $"actions= {FirstFailureAction}/{SecondFailureAction}/{SubsequentFailuresAction}";

        if (!string.IsNullOrEmpty(RebootMessage))
        {
            failureActions += $" reboot=\"{RebootMessage}\"";
        }

        if (!string.IsNullOrEmpty(RestartCommand))
        {
            failureActions += $" command=\"{RestartCommand}\"";
        }

        ExecuteServiceCommand(failureActions);

        if (Dependencies == null || !Dependencies.Any())
        {
            return;
        }

        var dependenciesStr = string.Join("/", Dependencies);
        ExecuteServiceCommand($"config {Name} depend= {dependenciesStr}");
    }

    public virtual void Start()
    {
        using var serviceController = new ServiceController(Name);

        if (serviceController.Status == ServiceControllerStatus.Running)
        {
            return;
        }

        serviceController.Start();
        serviceController.WaitForStatus(ServiceControllerStatus.Running);
    }

    public virtual void Stop()
    {
        using var serviceController = new ServiceController(Name);

        if (serviceController.Status == ServiceControllerStatus.Stopped)
        {
            return;
        }

        serviceController.Stop();
        serviceController.WaitForStatus(ServiceControllerStatus.Stopped);
    }

    public virtual void Restart()
    {
        throw new NotImplementedException();
    }

    public virtual void Delete() => ExecuteServiceCommand($"delete {Name}");

    protected virtual void ExecuteServiceCommand(string arguments)
    {
        var process = processWrapperFactory.Create();

        process.Start(new ProcessStartInfo
        {
            FileName = "sc",
            Arguments = arguments,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            Verb = "runas"
        });

        process.WaitForExit();
    }


    protected enum ServiceStartType
    {
        Boot,
        System,
        Auto,
        Demand,
        Disabled,
        DelayedAuto
    }

    protected class FailureAction
    {
        private ServiceFailureActionType ActionType { get; }

        private int Delay { get; }

        private FailureAction(ServiceFailureActionType actionType, int delay = 0)
        {
            ActionType = actionType;
            Delay = delay;
        }

        public static FailureAction Create(ServiceFailureActionType actionType, int delay = 0)
        {
            return new FailureAction(actionType, delay);
        }

        public static FailureAction None => new(ServiceFailureActionType.None);

        public override string ToString()
        {
            return ActionType switch
            {
                ServiceFailureActionType.Restart => $"restart/{Delay}",
                ServiceFailureActionType.Reboot => $"reboot/{Delay}",
                ServiceFailureActionType.RunCommand => $"run/{Delay}",
                ServiceFailureActionType.None => "none",
                _ => "none"
            };
        }
    }

    protected enum ServiceFailureActionType
    {
        None,
        Restart,
        Reboot,
        RunCommand
    }
}
