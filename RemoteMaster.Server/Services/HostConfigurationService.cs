// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Text;
using System.Text.Json;
using RemoteMaster.Server.Abstractions;
using RemoteMaster.Shared.Models;
using Serilog;

namespace RemoteMaster.Server.Services;

public class HostConfigurationService(JsonSerializerOptions jsonOptions) : IHostConfigurationService
{
    public async Task<MemoryStream> GenerateConfigFileAsync(HostConfiguration config)
    {
        try
        {
            var json = JsonSerializer.Serialize(config, jsonOptions);
            var jsonBytes = Encoding.UTF8.GetBytes(json);

            return await WriteToMemoryStreamAsync(jsonBytes);
        }
        catch (JsonException ex)
        {
            Log.Error(ex, "Error during JSON serialization.");
            throw new ApplicationException("Error during JSON serialization.", ex);
        }
    }

    private static async Task<MemoryStream> WriteToMemoryStreamAsync(byte[] bytes)
    {
        var memoryStream = new MemoryStream();

        try
        {
            await memoryStream.WriteAsync(bytes);
            Log.Information("Successfully generated config file.");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "An error occurred while writing to the memory stream.");
            throw;
        }

        return memoryStream;
    }
}
