// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

namespace RemoteMaster.Host.Core.Abstractions;

public interface IAppState
{
    event EventHandler<IViewer> ViewerAdded;

    event EventHandler<IViewer?> ViewerRemoved;

    event EventHandler<ICapturingContext>? CapturingContextAdded;

    event EventHandler<ICapturingContext?>? CapturingContextRemoved;

    IReadOnlyDictionary<string, IViewer> Viewers { get; }

    IReadOnlyDictionary<string, ICapturingContext> CapturingContexts { get; }

    bool TryGetViewer(string connectionId, out IViewer? viewer);

    bool TryAddViewer(IViewer viewer);

    bool TryRemoveViewer(string connectionId);

    IReadOnlyList<IViewer> GetAllViewers();

    bool TryGetCapturingContext(string connectionId, out ICapturingContext? capturingContext);

    bool TryAddCapturingContext(ICapturingContext capturingContext);

    bool TryRemoveCapturingContext(string connectionId);
}
