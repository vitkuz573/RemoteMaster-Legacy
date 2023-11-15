// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using Microsoft.AspNetCore.Components;

namespace RemoteMaster.Server.Components;

public partial class HostConfigurationGenerator
{
    private string _group;

    [Inject]
    public NavigationManager NavigationManager { get; set; }

    public void DownloadHost()
    {
        NavigationManager.NavigateTo("api/HostConfiguration/download-host", true);
    }
}
