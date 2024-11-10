// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

namespace RemoteMaster.Host.Core.Abstractions;

public interface ILaunchParameter
{
    string Name { get; }

    string Description { get; }

    bool IsRequired { get; }

    IReadOnlyList<string> Aliases { get; }

    object? Value { get; }

    object? GetValue(string[] args);

    void SetValue(string value);
}
