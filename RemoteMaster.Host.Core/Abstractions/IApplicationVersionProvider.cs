// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

namespace RemoteMaster.Host.Core.Abstractions;

public interface IApplicationVersionProvider
{
    /// <summary>
    /// Retrieves the version of the specified assembly.
    /// If no assembly name is provided, it retrieves the version of the entry assembly.
    /// </summary>
    /// <param name="assemblyName">The name of the assembly. If null or empty, the entry assembly is used.</param>
    /// <returns>A <see cref="Version"/> object representing the version, or <c>0.0.0.0</c> if not found.</returns>
    Version GetVersionFromAssembly(string? assemblyName = null);

    /// <summary>
    /// Retrieves the version of the specified executable file.
    /// </summary>
    /// <param name="executablePath">The full path to the executable file.</param>
    /// <returns>A <see cref="Version"/> object representing the version, or <c>0.0.0.0</c> if not found.</returns>
    Version GetVersionFromExecutable(string executablePath);
}
