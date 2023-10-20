// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

namespace RemoteMaster.Host.Core.Abstractions;

public interface IAppState
{
    event EventHandler<IViewer> ViewerAdded;

    event EventHandler<IViewer> ViewerRemoved;

    IReadOnlyDictionary<string, IViewer> Viewers { get; }

    bool TryGetViewer(string connectionId, out IViewer viewer);

    bool TryAddViewer(IViewer viewer);

    bool TryRemoveViewer(string connectionId, out IViewer viewer);
}
