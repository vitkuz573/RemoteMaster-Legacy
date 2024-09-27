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
        if (!System.Uri.IsWellFormedUriString(uri, UriKind.Absolute))
        {
            uri = ToAbsoluteUri(uri).ToString();
        }

        Uri = uri;
        ForceLoad = forceLoad;
    }

    public bool ForceLoad { get; private set; }
}
