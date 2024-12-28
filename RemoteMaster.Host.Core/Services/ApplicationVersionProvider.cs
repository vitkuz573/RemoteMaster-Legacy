// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Reflection;
using RemoteMaster.Host.Core.Abstractions;

namespace RemoteMaster.Host.Core.Services;

public class ApplicationVersionProvider(IAssemblyProvider assemblyProvider, IFileVersionInfoProvider fileVersionInfoProvider, IAssemblyAttributeProvider assemblyAttributeProvider) : IApplicationVersionProvider
{
    /// <summary>
    /// Retrieves the version of the specified assembly.
    /// If no assembly name is provided, it retrieves the version of the entry assembly.
    /// </summary>
    /// <param name="assemblyName">The name of the assembly. If null or empty, the entry assembly is used.</param>
    /// <returns>A <see cref="Version"/> object representing the version, or <c>0.0.0.0</c> if not found.</returns>
    /// <exception cref="ArgumentException">Thrown if the specified assembly is not found.</exception>
    /// <exception cref="InvalidOperationException">Thrown if the entry assembly cannot be determined.</exception>
    public Version GetVersionFromAssembly(string? assemblyName = null)
    {
        Assembly? assembly;

        if (string.IsNullOrWhiteSpace(assemblyName))
        {
            assembly = assemblyProvider.GetEntryAssembly();

            if (assembly == null)
            {
                throw new InvalidOperationException("Failed to retrieve the entry assembly.");
            }
        }
        else
        {
            assembly = assemblyProvider.GetAssemblyByName(assemblyName);

            if (assembly == null)
            {
                throw new ArgumentException($"Assembly with name '{assemblyName}' not found.", nameof(assemblyName));
            }
        }

        var fileVersion = assemblyAttributeProvider.GetCustomAttribute<AssemblyFileVersionAttribute>(assembly)?.Version;

        return Version.TryParse(fileVersion, out var version) ? version : new Version(0, 0, 0, 0);
    }

    /// <summary>
    /// Retrieves the version of the specified executable file.
    /// </summary>
    /// <param name="executablePath">The full path to the executable file.</param>
    /// <returns>A <see cref="Version"/> object representing the version, or <c>0.0.0.0</c> if not found.</returns>
    /// <exception cref="ArgumentException">Thrown if the executable path is null or empty.</exception>
    /// <exception cref="FileNotFoundException">Thrown if the executable file does not exist.</exception>
    public Version GetVersionFromExecutable(string executablePath)
    {
        if (string.IsNullOrWhiteSpace(executablePath))
        {
            throw new ArgumentException("Executable path cannot be null or empty.", nameof(executablePath));
        }

        if (!fileVersionInfoProvider.FileExists(executablePath))
        {
            throw new FileNotFoundException($"Executable file not found at path: {executablePath}", executablePath);
        }

        var fileVersion = fileVersionInfoProvider.GetFileVersion(executablePath);

        return Version.TryParse(fileVersion, out var version) ? version : new Version(0, 0, 0, 0);
    }
}
