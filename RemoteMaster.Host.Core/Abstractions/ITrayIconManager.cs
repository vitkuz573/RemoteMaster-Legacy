// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Drawing;

namespace RemoteMaster.Host.Core.Abstractions;

public interface ITrayIconManager : IDisposable
{
    void ShowTrayIcon();

    void HideTrayIcon();

    void UpdateIcon(Icon icon);

    void UpdateTooltip(string newTooltipText);

    void UpdateConnectionCount(int activeConnections);
}
