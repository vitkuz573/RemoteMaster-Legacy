// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

namespace RemoteMaster.Host.Core.Abstractions;

/// <summary>
/// Defines a contract for retrieving command line arguments from a specified process.
/// </summary>
public interface ICommandLineProvider
{
    /// <summary>
    /// Retrieves the command line arguments for the given process.
    /// </summary>
    /// <param name="process">The process for which to retrieve the command line arguments.</param>
    /// <returns>An array of command line arguments.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when the <paramref name="process"/> parameter is <c>null</c>.
    /// </exception>
    Task<string[]> GetCommandLineAsync(IProcess process);
}
