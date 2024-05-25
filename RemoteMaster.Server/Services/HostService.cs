// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Collections.ObjectModel;
using System.Management;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using System.Net;
using RemoteMaster.Server.Abstractions;
using Serilog;

namespace RemoteMaster.Server.Services;

public class HostService : IHostService
{
    private static WSManConnectionInfo CreateWSManConnectionInfo(string scheme, string host, int port, string appName, string shellUri, PSCredential credential)
    {
        var connectionInfo = new WSManConnectionInfo(scheme, host, port, appName, shellUri, credential)
        {
            AuthenticationMechanism = AuthenticationMechanism.Negotiate,
            UseCompression = true,
            NoMachineProfile = true,
            SkipCACheck = true,
            SkipCNCheck = true,
            SkipRevocationCheck = true
        };

        return connectionInfo;
    }

    private static void CopyFileToRemote(string localFilePath, string remoteFilePath, string host, PSCredential credential)
    {
        var connectionInfo = CreateWSManConnectionInfo(WSManConnectionInfo.HttpScheme, host, 5985, "/wsman", "http://schemas.microsoft.com/powershell/Microsoft.PowerShell", credential);

        using var runspace = RunspaceFactory.CreateRunspace(connectionInfo);
        runspace.Open();

        using var pipeline = runspace.CreatePipeline();
        var copyCommand = $"Copy-Item -Path \"{localFilePath}\" -Destination \"{remoteFilePath}\" -Force";
        pipeline.Commands.AddScript(copyCommand);

        var results = pipeline.Invoke();

        if (pipeline.Error.Count > 0)
        {
            if (pipeline.Error.Read() is Collection<ErrorRecord> error && error.Count > 0)
            {
                throw new Exception($"Error copying file: {error[0]}");
            }
        }

        Log.Information("File copied to remote path {RemoteFilePath}", remoteFilePath);
    }

    private static void ExecuteRemoteCommand(string remoteFilePath, string host, PSCredential credential, string launchMode)
    {
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
        inParams["CommandLine"] = $"{remoteFilePath} --launch-mode={launchMode}";

        var outParams = processClass.InvokeMethod("Create", inParams, null) ?? throw new Exception("Failed to execute process. The return value is null.");

        if ((uint)outParams["returnValue"] != 0)
        {
            throw new Exception($"Failed to execute process. Return value: {outParams["returnValue"]}");
        }

        Log.Information("Process ID: {ProcessId} for launch mode {LaunchMode}", outParams["processId"], launchMode);
    }

    public void DeployAndExecuteHost(string localFilePath, string remoteFilePath, string host, PSCredential credential, string launchMode)
    {
        ArgumentNullException.ThrowIfNull(localFilePath, nameof(localFilePath));
        ArgumentNullException.ThrowIfNull(remoteFilePath, nameof(remoteFilePath));
        ArgumentNullException.ThrowIfNull(host, nameof(host));
        ArgumentNullException.ThrowIfNull(credential, nameof(credential));
        ArgumentNullException.ThrowIfNull(launchMode, nameof(launchMode));

        CopyFileToRemote(localFilePath, remoteFilePath, host, credential);
        ExecuteRemoteCommand(remoteFilePath, host, credential, launchMode);
    }
}
