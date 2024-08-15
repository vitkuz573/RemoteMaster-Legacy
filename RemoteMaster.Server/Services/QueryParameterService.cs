// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using FluentResults;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Primitives;
using RemoteMaster.Server.Abstractions;

namespace RemoteMaster.Server.Services;

public class QueryParameterService(NavigationManager navigationManager) : IQueryParameterService
{
    public Result<T> GetParameter<T>(string key, T defaultValue)
    {
        try
        {
            var uri = new Uri(navigationManager.Uri);
            var queryParameters = QueryHelpers.ParseQuery(uri.Query);

            if (queryParameters.TryGetValue(key, out var valueString) && TryConvertValue(valueString, out T? value))
            {
                return Result.Ok(value);
            }

            return Result.Ok(defaultValue);
        }
        catch (Exception ex)
        {
            return Result.Fail<T>($"Error retrieving query parameter '{key}': {ex.Message}").WithError(ex.Message);
        }
    }

    private static bool TryConvertValue<T>(StringValues stringValue, out T? result)
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

    public Result UpdateParameter(string key, object value)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(value);

            var uri = new Uri(navigationManager.Uri);
            var queryParameters = QueryHelpers.ParseQuery(uri.Query);
            queryParameters[key] = value.ToString();

            var newUri = QueryHelpers.AddQueryString(uri.GetLeftPart(UriPartial.Path), queryParameters);
            navigationManager.NavigateTo(newUri);

            return Result.Ok();
        }
        catch (Exception ex)
        {
            return Result.Fail($"Error updating query parameter '{key}': {ex.Message}").WithError(ex.Message);
        }
    }
}
