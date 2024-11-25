// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using RemoteMaster.Shared.DTOs;
using RemoteMaster.Shared.Enums;
using RemoteMaster.Shared.Models;

namespace RemoteMaster.Host.Core.Abstractions;

public interface IControlClient
{
    Task ReceiveThumbnail(byte[] thumbnail);

    Task ReceiveScreenUpdate(byte[] screenUpdate);

    Task ReceiveDisplays(IEnumerable<Display> displays);

    Task ReceiveMessage(Message message);

    Task ReceiveCommand(string command);

    Task ReceiveHostVersion(string version);

    Task ReceiveCloseConnection();

    Task ReceiveTransportType(string transportType);

    Task ReceiveAllViewers(List<ViewerDto> viewers);

    Task ReceiveAvailableCodecs(IEnumerable<string> codecs);

    Task ReceiveDisconnected(DisconnectReason reason);

    Task ReceiveDotNetVersion(Version version);

    Task ReceiveOperatingSystemVersion(string version);
}
