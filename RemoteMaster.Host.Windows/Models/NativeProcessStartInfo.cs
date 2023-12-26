// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Text;

namespace RemoteMaster.Host.Windows.Models;

public class NativeProcessStartInfo(string applicationName, int targetSessionId)
{
    public string FileName { get; init; } = applicationName;

    public string? Arguments { get; init; } = null;

    public int TargetSessionId { get; init; } = targetSessionId;

    public bool ForceConsoleSession { get; init; } = true;

    public string DesktopName { get; init; } = "Default";

    public bool CreateNoWindow { get; init; } = true;

    public bool UseCurrentUserToken { get; init; } = false;

    public bool RedirectStandardInput { get; init; }
    
    public bool RedirectStandardOutput { get; init; }

    public bool RedirectStandardError { get; init; }

    public Encoding? StandardInputEncoding { get; init; }

    public Encoding? StandardOutputEncoding { get; init; }

    public Encoding? StandardErrorEncoding { get; init; }
}