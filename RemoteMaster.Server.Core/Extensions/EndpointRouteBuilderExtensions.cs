// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// Unauthorized copying of this file, via any medium is strictly prohibited.

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