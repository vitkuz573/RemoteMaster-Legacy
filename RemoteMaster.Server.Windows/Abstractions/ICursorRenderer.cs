// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Drawing;

namespace RemoteMaster.Server.Abstractions;

public interface ICursorRenderer : IDisposable
{
    event Func<Rectangle> RequestScreenBounds;

    void DrawCursor(Graphics g);
}
