// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Collections.Specialized;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace RemoteMaster.Host.Windows.Models;

public class NativeProcessStartInfo
{
    private string? _fileName;
    private string? _arguments;

    internal Dictionary<string, string?> _environmentVariables;

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

    public int? TargetSessionId { get; set; }

    public bool ForceConsoleSession { get; set; } = true;

    public string DesktopName { get; set; } = "Default";

    public bool CreateNoWindow { get; set; } = true;

    public StringDictionary EnvironmentVariables
    {
        get
        {
            var environmentVariables = new StringDictionary();

            if (Environment is Dictionary<string, string?> dictionary)
            {
                foreach (var entry in dictionary)
                {
                    environmentVariables[entry.Key] = entry.Value;
                }
            }

            return environmentVariables;
        }
    }

    public IDictionary<string, string?> Environment
    {
        get
        {
            if (_environmentVariables == null)
            {
                var envVars = System.Environment.GetEnvironmentVariables();

                _environmentVariables = new Dictionary<string, string?>(new Dictionary<string, string?>(envVars.Count, OperatingSystem.IsWindows() ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal));

                var e = envVars.GetEnumerator();
                Debug.Assert(e is not IDisposable, "Environment.GetEnvironmentVariables should not be IDisposable.");

                while (e.MoveNext())
                {
                    var entry = e.Entry;
                    _environmentVariables.Add((string)entry.Key, (string?)entry.Value);
                }
            }

            return _environmentVariables;
        }
    }

    public bool UseCurrentUserToken { get; set; } = false;

    public bool RedirectStandardInput { get; set; }

    public bool RedirectStandardOutput { get; set; }

    public bool RedirectStandardError { get; set; }

    public Encoding? StandardInputEncoding { get; set; }

    public Encoding? StandardOutputEncoding { get; set; }

    public Encoding? StandardErrorEncoding { get; set; }
}