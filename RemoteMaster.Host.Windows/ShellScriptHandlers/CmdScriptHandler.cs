// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Text;
using RemoteMaster.Host.Core.Abstractions;

namespace RemoteMaster.Host.Windows.ShellScriptHandlers;

public class CmdScriptHandler : IShellScriptHandler
{
    public string FileExtension => ".bat";
    
    public Encoding FileEncoding => new UTF8Encoding(false);
    
    public string GetExecutionCommand(string scriptFilePath) => $"cmd /c \"{scriptFilePath}\"";
    
    public string FormatScript(string scriptContent) => $"@echo off\r\n{scriptContent}";
}
