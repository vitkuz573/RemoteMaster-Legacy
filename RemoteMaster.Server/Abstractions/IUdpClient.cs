// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Net;

namespace RemoteMaster.Server.Abstractions;

public interface IUdpClient : IDisposable
{
    bool EnableBroadcast { get; set; }

    int Send(byte[] datagram, int bytes, IPEndPoint endPoint);
}
