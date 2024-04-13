// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using Microsoft.Extensions.DependencyInjection;
using RemoteMaster.Shared.Abstractions;
using RemoteMaster.Shared.Services;

namespace RemoteMaster.Shared.Extensions;

public static class ServiceCollectionExtensions
{
    public static void AddSharedServices(this IServiceCollection services)
    {
        services.AddSingleton<ISubjectService, SubjectService>();
        services.AddSingleton<IHostInformationService, HostInformationService>();
    }
}
