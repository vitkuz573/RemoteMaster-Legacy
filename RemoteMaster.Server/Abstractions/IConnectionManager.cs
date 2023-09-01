// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

namespace RemoteMaster.Server.Abstractions;

public interface IConnectionManager : IDisposable
{
    IConnectionContext Connect(string name, string url, bool useMessagePack = false);

    IConnectionContext? Get(string name);

    Task DisconnectAsync(string name);
}
