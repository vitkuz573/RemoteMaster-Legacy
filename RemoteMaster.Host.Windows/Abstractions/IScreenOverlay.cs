// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Drawing;

namespace RemoteMaster.Host.Windows.Abstractions;

public interface IScreenOverlay : IDisposable
{
    string Name { get; }

    void Draw(Graphics graphics, Rectangle screenBounds);
}
