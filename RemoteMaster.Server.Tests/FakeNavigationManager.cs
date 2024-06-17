// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using Microsoft.AspNetCore.Components;

namespace RemoteMaster.Server.Tests;

public class FakeNavigationManager : NavigationManager
{
    public FakeNavigationManager(string baseUri, string uri)
    {
        ArgumentNullException.ThrowIfNull(baseUri);

        Initialize(baseUri.EndsWith('/') ? baseUri : baseUri + "/", uri);
    }

    protected override void NavigateToCore(string uri, bool forceLoad)
    {
        Uri = uri;
    }
}
