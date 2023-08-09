// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Web;
using Microsoft.AspNetCore.Components;
using RemoteMaster.Client.Abstractions;

namespace RemoteMaster.Client.Services;

public class UriParametersService : IUriParametersService
{
    private readonly NavigationManager _navManager;

    public UriParametersService(NavigationManager navManager)
    {
        _navManager = navManager;
    }

    public bool GetBoolParameter(string name)
    {
        var uri = new Uri(_navManager.Uri);
        var query = HttpUtility.ParseQueryString(uri.Query);
        var paramValue = query.Get(name);

        return bool.TryParse(paramValue, out var result) && result;
    }
    
    public string GetStringParameter(string name)
    {
        var uri = new Uri(_navManager.Uri);
        var query = HttpUtility.ParseQueryString(uri.Query);

        return query.Get(name);
    }
}
