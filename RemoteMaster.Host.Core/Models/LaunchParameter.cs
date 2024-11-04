// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using RemoteMaster.Host.Core.Abstractions;

namespace RemoteMaster.Host.Core.Models;

public class LaunchParameter(string description, bool isRequired, params string[] aliases) : ILaunchParameter
{
    public string Description { get; } = description;

    public bool IsRequired { get; } = isRequired;

    public string? Value { get; set; }

    public IReadOnlyList<string> Aliases { get; } = aliases;
}
