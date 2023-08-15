// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Web;
using Microsoft.AspNetCore.Components;
using RemoteMaster.Client.Abstractions;

namespace RemoteMaster.Client.Services;

public class QueryParameterService : IQueryParameterService
{
    private readonly NavigationManager _navigationManager;

    public QueryParameterService(NavigationManager navigationManager)
    {
        _navigationManager = navigationManager ?? throw new ArgumentNullException(nameof(navigationManager));
    }

    public T GetValueFromQuery<T>(string parameterName)
    {
        var uri = new Uri(_navigationManager.Uri);
        var query = HttpUtility.ParseQueryString(uri.Query);
        var parameterValue = query[parameterName];

        if (parameterValue == null)
        {
            return default;
        }

        return (T)Convert.ChangeType(parameterValue, typeof(T));
    }
}