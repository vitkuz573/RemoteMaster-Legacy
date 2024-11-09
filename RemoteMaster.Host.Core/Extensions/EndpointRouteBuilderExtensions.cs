// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using RemoteMaster.Host.Core.Abstractions;
using RemoteMaster.Host.Core.Hubs;
using RemoteMaster.Host.Core.LaunchModes;

namespace RemoteMaster.Host.Core.Extensions;

public static class EndpointRouteBuilderExtensions
{
    public static void MapCoreHubs(this IEndpointRouteBuilder endpoints, LaunchModeBase launchModeBase)
    {
        if (launchModeBase is UserMode)
        {
            endpoints.MapHub<ControlHub>("/hubs/control");
            endpoints.MapHub<CertificateHub>("/hubs/certificate");
            endpoints.MapHub<FileManagerHub>("/hubs/filemanager");
            endpoints.MapHub<TaskManagerHub>("/hubs/taskmanager");
            endpoints.MapHub<ScreenRecorderHub>("/hubs/screenrecorder");
            endpoints.MapHub<DomainMembershipHub>("/hubs/domainmembership");
            endpoints.MapHub<ChatHub>("/hubs/chat");
            endpoints.MapHub<LogHub>("/hubs/log");
        }

        if (launchModeBase is UserMode or UpdaterMode)
        {
            endpoints.MapHub<UpdaterHub>("/hubs/updater");
        }
    }
}