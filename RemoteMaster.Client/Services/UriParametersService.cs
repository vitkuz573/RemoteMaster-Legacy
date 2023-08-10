// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.ComponentModel;
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

    public TType GetParameter<TType>(string name)
    {
        var uri = new Uri(_navManager.Uri);
        var query = HttpUtility.ParseQueryString(uri.Query);
        var paramValue = query.Get(name);

        var converter = TypeDescriptor.GetConverter(typeof(TType));
        
        if (converter != null && converter.CanConvertFrom(typeof(string)))
        {
            try
            {
                return (TType)converter.ConvertFromString(paramValue);
            }
            catch (Exception)
            {
                // Здесь можно обработать исключения, возникающие при конвертации.
                // Возможно, добавить логирование или вернуть default.
            }
        }

        return default;
    }
}
