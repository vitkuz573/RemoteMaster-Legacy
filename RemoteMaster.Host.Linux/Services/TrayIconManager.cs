// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Drawing;
using RemoteMaster.Host.Core.Abstractions;

namespace RemoteMaster.Host.Linux.Services;

public class TrayIconManager : ITrayIconManager
{
    private bool _iconAdded;

    public void Show()
    {
        if (!_iconAdded)
        {
            AddTrayIcon();
        }
    }

    public void Hide() { }

    public void SetIcon(Icon icon) { }

    public void SetTooltip(string tooltip) { }

    private static void AddTrayIcon() { }

    public void Dispose() { }
}
