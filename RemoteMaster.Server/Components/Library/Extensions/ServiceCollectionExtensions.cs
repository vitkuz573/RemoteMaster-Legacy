// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using RemoteMaster.Server.Components.Library.Abstractions;
using RemoteMaster.Server.Components.Library.Services;

namespace RemoteMaster.Server.Components.Library.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddThemeProvider(this IServiceCollection services)
    {
        services.AddSingleton<IThemeService, ThemeService>();

        return services;
    }

    public static IServiceCollection AddDialogService(this IServiceCollection services)
    {
        services.AddScoped<IDialogWindowService, DialogService>();

        return services;
    }

    public static IServiceCollection AddLibraryServices(this IServiceCollection services)
    {
        services.AddThemeProvider();
        services.AddDialogService();

        return services;
    }
}
