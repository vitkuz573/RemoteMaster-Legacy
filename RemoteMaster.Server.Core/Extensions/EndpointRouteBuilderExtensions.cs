using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using RemoteMaster.Server.Core.Hubs;

namespace RemoteMaster.Server.Core.Extensions;

public static class EndpointRouteBuilderExtensions
{
    public static IEndpointRouteBuilder MapCoreHubs(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapHub<ControlHub>("/hubs/control");

        return endpoints;
    }
}