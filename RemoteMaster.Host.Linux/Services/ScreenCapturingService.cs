// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using RemoteMaster.Host.Core.Abstractions;
using RemoteMaster.Shared.Models;

namespace RemoteMaster.Host.Linux.Services;

public class ScreenCapturingService : IScreenCapturingService
{
    public byte[]? GetNextFrame(string connectionId) => throw new NotImplementedException();

    public IEnumerable<Display> GetDisplays() => throw new NotImplementedException();

    public IScreen? FindScreenByName(string displayName) => throw new NotImplementedException();

    public void SetSelectedScreen(string connectionId, IScreen display) => throw new NotImplementedException();

    public byte[]? GetThumbnail(string connectionId) => throw new NotImplementedException();

    public void Dispose() => throw new NotImplementedException();
}
