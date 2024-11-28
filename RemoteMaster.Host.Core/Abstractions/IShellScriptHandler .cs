// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Text;

namespace RemoteMaster.Host.Core.Abstractions;

public interface IShellScriptHandler
{
    string FileExtension { get; }
    
    Encoding FileEncoding { get; }

    string FormatScript(string content);

    string ExecutableName { get; }

    string GetArguments(string scriptPath);
}
