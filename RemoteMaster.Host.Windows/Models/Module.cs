// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using RemoteMaster.Host.Core.Abstractions;
using RemoteMaster.Host.Core.Models;
using RemoteMaster.Host.Windows.Services;

namespace RemoteMaster.Host.Windows.Models;

public class Module(ModuleInfo moduleInfo, string moduleDirectory) : IModule
{
    private NativeProcess? _nativeProcess;

    public void Start()
    {
        var fullEntryPointPath = Path.Combine(moduleDirectory, moduleInfo.EntryPoint);

        var startInfo = new NativeProcessStartInfo
        {
            FileName = fullEntryPointPath,
            ForceConsoleSession = true,
            CreateNoWindow = false,
            UseCurrentUserToken = false,
            Environment =
            {
                ["WEBVIEW2_USER_DATA_FOLDER"] = @"C:\ProgramData\RemoteMaster\Host"
            }
        };

        _nativeProcess = new NativeProcess();
        _nativeProcess.StartInfo = startInfo;
        _nativeProcess.Start();
    }

    public bool IsRunning()
    {
        if (_nativeProcess == null)
        {
            return false;
        }

        return !_nativeProcess.HasExited;
    }

    public void Load()
    {

    }

    public void Initialize()
    {

    }

    public ModuleInfo GetModuleInfo()
    {
        return moduleInfo;
    }
}