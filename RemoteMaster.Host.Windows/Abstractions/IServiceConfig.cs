// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

namespace RemoteMaster.Host.Abstractions;

public interface IServiceConfig
{
    string Name { get; }

    string DisplayName { get; }

    string StartType { get; }

    IEnumerable<string>? Dependencies { get; }
}
