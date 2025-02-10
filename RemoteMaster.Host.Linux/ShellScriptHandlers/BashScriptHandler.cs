// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Text;
using RemoteMaster.Host.Core.Abstractions;

namespace RemoteMaster.Host.Linux.ShellScriptHandlers;

public class BashScriptHandler : IShellScriptHandler
{
    public string FileExtension => ".sh";

    public Encoding FileEncoding => new UTF8Encoding(false);

    public string FormatScript(string content) => $"#/bin/bash\r\n{content}";

    public string ExecutableName => "sh";

    public string GetArguments(string scriptPath) => $"\"{scriptPath}\"";
}
