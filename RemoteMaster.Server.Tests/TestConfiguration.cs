// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using Microsoft.Extensions.Configuration;

namespace RemoteMaster.Server.Tests;

public static class TestConfiguration
{
    public static IConfiguration GetTestConfiguration()
    {
        var inMemorySettings = new Dictionary<string, string>
        {
            { "ConnectionStrings:DefaultConnection", "Data Source=:memory:" }
        };

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings)
            .Build();

        return configuration;
    }
}
