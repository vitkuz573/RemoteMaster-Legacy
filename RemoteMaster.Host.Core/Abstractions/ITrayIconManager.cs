// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

namespace RemoteMaster.Host.Core.Abstractions;

public interface ITrayIconManager : IDisposable
{
    void ShowTrayIcon();

    void HideTrayIcon();

    void UpdateIcon(string iconPath, uint iconIndex = 0);

    void UpdateTooltip(string newTooltipText);

    void UpdateConnectionCount(int activeConnections);
}
