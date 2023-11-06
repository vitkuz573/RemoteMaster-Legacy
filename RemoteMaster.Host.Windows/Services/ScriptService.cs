// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using RemoteMaster.Host.Core.Abstractions;
using RemoteMaster.Host.Windows.Models;
using RemoteMaster.Shared.Models;
using Serilog;

namespace RemoteMaster.Host.Windows.Services;

public class ScriptService : IScriptService
{
    public async void Execute(Shell shell, string script)
    {
        Log.Information("Executing script with shell: {Shell}", shell);

        var publicDirectory = @"C:\Users\Public";
        var fileName = $"{Guid.NewGuid()}";

        if (shell == Shell.Cmd)
        {
            fileName += ".bat";
        }
        else if (shell == Shell.Cmd)
        {
            fileName += ".ps1";
        }
        else
        {
            Log.Error("Unsupported shell encountered: {Shell}", shell);

            throw new InvalidOperationException($"Unsupported shell: {shell}");
        }

        var tempFilePath = Path.Combine(publicDirectory, fileName);

        Log.Information("Temporary file path: {TempFilePath}", tempFilePath);
        File.WriteAllText(tempFilePath, script);

        try
        {
            if (!File.Exists(tempFilePath))
            {
                Log.Error("Temp file was not created: {TempFilePath}", tempFilePath);

                return;
            }

            var applicationToRun = shell switch
            {
                Shell.Cmd => $"cmd.exe /c \"{tempFilePath}\"",
                Shell.PowerShell => $"powershell.exe -ExecutionPolicy Bypass -File \"{tempFilePath}\"",
                _ => "",
            };

            var options = new NativeProcessStartInfo(applicationToRun, -1)
            {
                ForceConsoleSession = true,
                DesktopName = "default",
                CreateNoWindow = true,
                UseCurrentUserToken = true,
                InheritHandles = true
            };

            NativeProcess.Start(options);
        }
        finally
        {
            await Task.Delay(5000);

            if (File.Exists(tempFilePath))
            {
                File.Delete(tempFilePath);
            }
        }
    }
}
