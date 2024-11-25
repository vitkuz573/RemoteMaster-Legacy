// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Text;
using RemoteMaster.Host.Core.Abstractions;

namespace RemoteMaster.Host.Windows.ShellScriptHandlers;

public class PowerShellScriptHandler : IShellScriptHandler
{
    public string FileExtension => ".ps1";
    
    public Encoding FileEncoding => new UTF8Encoding(true);
    
    public string GetExecutionCommand(string scriptFilePath) => $"powershell -ExecutionPolicy Bypass -File \"{scriptFilePath}\"";
    
    public string FormatScript(string scriptContent) => scriptContent;
}
