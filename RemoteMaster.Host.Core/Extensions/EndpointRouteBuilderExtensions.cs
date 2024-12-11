// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using RemoteMaster.Host.Core.Hubs;

namespace RemoteMaster.Host.Core.Extensions;

public static class EndpointRouteBuilderExtensions
{
    public static void MapCoreHubs(this IEndpointRouteBuilder endpoints, string commandName)
    {
        if (commandName == "user")
        {
            endpoints.MapHub<ControlHub>("/hubs/control");
            endpoints.MapHub<CertificateHub>("/hubs/certificate");
            endpoints.MapHub<FileManagerHub>("/hubs/filemanager");
            endpoints.MapHub<TaskManagerHub>("/hubs/taskmanager");
            endpoints.MapHub<ScreenRecorderHub>("/hubs/screenrecorder");
            endpoints.MapHub<DomainMembershipHub>("/hubs/domainmembership");
            endpoints.MapHub<ChatHub>("/hubs/chat");
            endpoints.MapHub<LogHub>("/hubs/log");
            endpoints.MapHub<ManagementHub>("/hubs/management");
        }

        if (commandName is "user" or "update")
        {
            endpoints.MapHub<UpdaterHub>("/hubs/updater");
        }
    }
}
