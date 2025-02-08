// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

namespace RemoteMaster.Host.Linux.Models;

/// <summary>
/// Represents a dependency with a name, a command to check if it's installed,
/// and a command to install it.
/// </summary>
public class Dependency
{
    /// <summary>
    /// The name of the dependency (e.g., "curl").
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// The command used to check if the dependency is installed (e.g., "which curl").
    /// </summary>
    public string CheckCommand { get; set; }

    /// <summary>
    /// The command used to install the dependency (e.g., "sudo apt-get install -y curl").
    /// </summary>
    public string InstallCommand { get; set; }
}
