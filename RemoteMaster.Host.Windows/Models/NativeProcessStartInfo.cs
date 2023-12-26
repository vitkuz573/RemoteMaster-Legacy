// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace RemoteMaster.Host.Windows.Models;

public class NativeProcessStartInfo
{
    private string? _fileName;
    private string? _arguments;

    public NativeProcessStartInfo()
    {
    }

    public NativeProcessStartInfo(string fileName)
    {
        _fileName = fileName;
    }

    public NativeProcessStartInfo(string fileName, string arguments)
    {
        _fileName = fileName;
        _arguments = arguments;
    }

    [AllowNull]
    public string FileName
    {
        get => _fileName ?? string.Empty;
        set => _fileName = value;
    }

    [AllowNull]
    public string Arguments
    {
        get => _arguments ?? string.Empty;
        set => _arguments = value;
    }

    public int TargetSessionId { get; set; }

    public bool ForceConsoleSession { get; set; } = true;

    public string DesktopName { get; set; } = "Default";

    public bool CreateNoWindow { get; set; } = true;

    public bool UseCurrentUserToken { get; set; } = false;

    public bool RedirectStandardInput { get; set; }
    
    public bool RedirectStandardOutput { get; set; }

    public bool RedirectStandardError { get; set; }

    public Encoding? StandardInputEncoding { get; set; }

    public Encoding? StandardOutputEncoding { get; set; }

    public Encoding? StandardErrorEncoding { get; set; }
}