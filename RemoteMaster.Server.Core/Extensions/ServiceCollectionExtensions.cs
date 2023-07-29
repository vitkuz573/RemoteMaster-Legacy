using Microsoft.Extensions.DependencyInjection;
using RemoteMaster.Server.Core.Abstractions;
using RemoteMaster.Server.Core.Services;

namespace RemoteMaster.Server.Core.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddCoreServices(this IServiceCollection services)
    {
        services.AddSingleton<IViewerStore, ViewerStore>();
        services.AddSingleton<IShutdownService, ShutdownService>();
        services.AddSingleton<IIdleTimer, IdleTimer>();
        services.AddScoped<IScreenCaster, ScreenCaster>();
        services.AddTransient<IViewerFactory, ViewerFactory>();

        return services;
    }
}
