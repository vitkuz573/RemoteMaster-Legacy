// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using Microsoft.AspNetCore.Components;

namespace RemoteMaster.Server.Components.Pages;

public partial class AccessNew
{
    [Parameter]
    public string Host { get; set; }

    private bool _isSidebarOpen = false;
    private string _activePanel = string.Empty;

    private void ToggleSidebar()
    {
        _isSidebarOpen = !_isSidebarOpen;
    }

    private void TogglePanel(string panelName)
    {
        _activePanel = _activePanel == panelName ? string.Empty : panelName;
    }
}
