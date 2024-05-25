// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Management;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using System.Net;
using RemoteMaster.Server.Abstractions;
using Serilog;

namespace RemoteMaster.Server.Services;

public class RemoteCommandService : IRemoteCommandService
{
    private static WSManConnectionInfo CreateWSManConnectionInfo(string scheme, string host, int port, string appName, string shellUri, PSCredential credential)
    {
        return new WSManConnectionInfo(scheme, host, port, appName, shellUri, credential)
        {
            AuthenticationMechanism = AuthenticationMechanism.Negotiate,
            UseCompression = true,
            NoMachineProfile = true,
            SkipCACheck = true,
            SkipCNCheck = true,
            SkipRevocationCheck = true
        };
    }

    private static Runspace CreateRunspace(WSManConnectionInfo connectionInfo)
    {
        var runspace = RunspaceFactory.CreateRunspace(connectionInfo);
        runspace.Open();

        return runspace;
    }

    private static void ExecutePipeline(Runspace runspace, string command)
    {
        using var pipeline = runspace.CreatePipeline();
        pipeline.Commands.AddScript(command);

        var results = pipeline.Invoke();

        if (pipeline.Error.Count > 0)
        {
            var errors = pipeline.Error.ReadToEnd().Cast<object>().ToList();

            if (errors.Count > 0)
            {
                throw new Exception($"Error executing command: {string.Join(", ", errors)}");
            }
        }
    }

    public void ExecuteCommand(string command, string host, PSCredential credential)
    {
        var connectionInfo = CreateWSManConnectionInfo(WSManConnectionInfo.HttpScheme, host, 5985, "/wsman", "http://schemas.microsoft.com/powershell/Microsoft.PowerShell", credential);
        using var runspace = CreateRunspace(connectionInfo);

        ExecutePipeline(runspace, command);

        Log.Information("Command executed on remote host {Host}: {Command}", host, command);
    }

    private static void CopyFileToRemote(string localFilePath, string remoteFilePath, string host, PSCredential credential)
    {
        var connectionInfo = CreateWSManConnectionInfo(WSManConnectionInfo.HttpScheme, host, 5985, "/wsman", "http://schemas.microsoft.com/powershell/Microsoft.PowerShell", credential);
        using var runspace = CreateRunspace(connectionInfo);

        var copyCommand = $"Copy-Item -Path \"{localFilePath}\" -Destination \"{remoteFilePath}\" -Force";
        ExecutePipeline(runspace, copyCommand);

        Log.Information("File copied to remote path {RemoteFilePath} on host {Host}", remoteFilePath, host);
    }

    private static void ExecuteRemoteProcess(string remoteFilePath, string host, PSCredential credential, params string[] args)
    {
        ArgumentNullException.ThrowIfNull(credential);

        var options = new ConnectionOptions
        {
            Impersonation = ImpersonationLevel.Impersonate,
            Username = credential.UserName,
            Password = new NetworkCredential("", credential.Password).Password
        };

        var scope = new ManagementScope($@"\\{host}\root\cimv2", options);
        scope.Connect();

        using var processClass = new ManagementClass(scope, new ManagementPath("Win32_Process"), new ObjectGetOptions());

        var inParams = processClass.GetMethodParameters("Create");
        inParams["CommandLine"] = $"{remoteFilePath} {string.Join(" ", args)}";

        var outParams = processClass.InvokeMethod("Create", inParams, null) ?? throw new Exception("Failed to execute process. The return value is null.");

        var returnValue = (uint)outParams["returnValue"];

        if (returnValue != 0)
        {
            throw new Exception($"Failed to execute process. Return value: {returnValue}");
        }

        Log.Information("Process ID: {ProcessId} for command {Command} on host {Host}", outParams["processId"], $"{remoteFilePath} {string.Join(" ", args)}", host);
    }

    public void DeployAndExecute(string localFilePath, string remoteFilePath, string host, PSCredential credential, params string[] args)
    {
        ArgumentNullException.ThrowIfNull(localFilePath, nameof(localFilePath));
        ArgumentNullException.ThrowIfNull(remoteFilePath, nameof(remoteFilePath));
        ArgumentNullException.ThrowIfNull(host, nameof(host));
        ArgumentNullException.ThrowIfNull(credential, nameof(credential));

        CopyFileToRemote(localFilePath, remoteFilePath, host, credential);
        ExecuteRemoteProcess(remoteFilePath, host, credential, args);
    }
}
