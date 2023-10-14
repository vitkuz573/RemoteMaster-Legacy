// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Drawing;

namespace RemoteMaster.Host.Abstractions;

public interface ICursorRenderService : IDisposable
{
    event Func<Rectangle> RequestScreenBounds;

    void DrawCursor(Graphics g);

    void UpdateScreenBounds(Rectangle newBounds);
}
