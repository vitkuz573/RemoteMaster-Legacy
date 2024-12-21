// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Reflection;
using RemoteMaster.Host.Core.Abstractions;

namespace RemoteMaster.Host.Core.Services;

public class ApplicationVersionProvider : IApplicationVersionProvider
{
    public string GetVersion()
    {
        var assembly = Assembly.GetEntryAssembly();
        var fileVersion = assembly?
            .GetCustomAttribute<AssemblyFileVersionAttribute>()?
            .Version;

        return fileVersion ?? "Unknown";
    }
}
