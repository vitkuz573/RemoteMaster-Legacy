// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using RemoteMaster.Host.Core.Hubs;

namespace RemoteMaster.Host.Core.Extensions;

public static class EndpointRouteBuilderExtensions
{
    public static IEndpointRouteBuilder MapCoreHubs(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapHub<ControlHub>("/hubs/control");
        endpoints.MapHub<MaintenanceHub>("/hubs/maintenance");

        return endpoints;
    }
}