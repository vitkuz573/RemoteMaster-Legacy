// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

namespace RemoteMaster.Host.Linux.Models;

/// <summary>
/// Configuration for the list of dependencies.
/// </summary>
public class DependencyConfig
{
    public List<Dependency> Dependencies { get; } = [];
}
