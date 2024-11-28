// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Text;
using RemoteMaster.Host.Core.Abstractions;

namespace RemoteMaster.Host.Windows.ShellScriptHandlers;

public class PwshScriptHandler : IShellScriptHandler
{
    public string FileExtension => ".ps1";

    public Encoding FileEncoding => new UTF8Encoding(true);

    public string FormatScript(string content) => content;

    public string ExecutableName => "pwsh";

    public string GetArguments(string scriptPath) => $"-ExecutionPolicy Bypass -File \"{scriptPath}\"";
}