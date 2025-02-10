// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using RemoteMaster.Host.Core.Abstractions;
using RemoteMaster.Host.Linux.ShellScriptHandlers;
using RemoteMaster.Shared.Enums;

namespace RemoteMaster.Host.Linux.Services;

public class ShellScriptHandlerFactory : IShellScriptHandlerFactory
{
    public IShellScriptHandler Create(Shell shell) => shell switch
    {
        Shell.Bash => new BashScriptHandler()
    };
}
