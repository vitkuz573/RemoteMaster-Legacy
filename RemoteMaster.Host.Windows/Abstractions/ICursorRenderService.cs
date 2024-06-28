// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Drawing;

namespace RemoteMaster.Host.Windows.Abstractions;

public interface ICursorRenderService : IDisposable
{
    void DrawCursor(Graphics g, Rectangle currentScreenBounds);
}
