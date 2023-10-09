// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using Microsoft.AspNetCore.Components;

namespace RemoteMaster.Server.Components;

public partial class ClientConfigurationGenerator
{
    private string _group;

    [Inject]
    public NavigationManager NavigationManager { get; set; }

    public void DownloadAgent()
    {
        NavigationManager.NavigateTo("api/ClientConfiguration/download-agent", true);
    }
}
