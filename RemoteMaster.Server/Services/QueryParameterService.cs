// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Primitives;
using RemoteMaster.Server.Abstractions;

namespace RemoteMaster.Server.Services;

public class QueryParameterService(NavigationManager navigationManager) : IQueryParameterService
{
    public T GetParameter<T>(string key, T defaultValue)
    {
        var uri = new Uri(navigationManager.Uri);
        var queryParameters = QueryHelpers.ParseQuery(uri.Query);

        if (queryParameters.TryGetValue(key, out var valueString) && TryConvertValue(valueString, out T value))
        {
            return value;
        }

        return defaultValue;
    }

    private static bool TryConvertValue<T>(StringValues stringValue, out T result)
    {
        try
        {
            result = (T)Convert.ChangeType(stringValue.ToString(), typeof(T));

            return true;
        }
        catch
        {
            result = default;

            return false;
        }
    }

    public void UpdateParameter(string key, object value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var uri = new Uri(navigationManager.Uri);
        var queryParameters = QueryHelpers.ParseQuery(uri.Query);
        queryParameters[key] = value.ToString();

        var newUri = QueryHelpers.AddQueryString(uri.GetLeftPart(UriPartial.Path), queryParameters);
        navigationManager.NavigateTo(newUri);
    }
}
