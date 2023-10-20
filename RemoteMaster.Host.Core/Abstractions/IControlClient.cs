// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Drawing;
using RemoteMaster.Shared.Dtos;

namespace RemoteMaster.Host.Core.Abstractions;

public interface IControlClient
{
    Task ReceiveThumbnail(byte[] thumbnail);

    Task ReceiveScreenUpdate(byte[] screenUpdate);

    Task ReceiveScreenData(ScreenDataDto screenData);

    Task ReceiveScreenSize(Size size);

    Task ReceiveScriptResult(string result);

    Task ReceiveCommand(string command);

    Task ReceiveHostVersion(Version version);
}
