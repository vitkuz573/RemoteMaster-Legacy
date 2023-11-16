// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using RemoteMaster.Server.Abstractions;
using RemoteMaster.Shared.Models;
using Serilog;

namespace RemoteMaster.Server.Services;

/// <summary>
/// Service responsible for generating host configuration files.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="HostConfigurationService"/> class.
/// </remarks>
/// <param name="serializationService">The serialization service instance.</param>
public class HostConfigurationService(ISerializationService serializationService) : IHostConfigurationService
{

    /// <summary>
    /// Generates a configuration file for the host based on the given configuration model.
    /// </summary>
    /// <param name="config">The configuration model.</param>
    /// <returns>A memory stream containing the configuration file.</returns>
    public async Task<MemoryStream> GenerateConfigFileAsync(HostConfiguration config)
    {
        var jsonBytes = serializationService.SerializeToJsonBytes(config);

        return await WriteToMemoryStreamAsync(jsonBytes);
    }

    /// <summary>
    /// Writes the given bytes to a memory stream.
    /// </summary>
    /// <param name="bytes">The bytes to write.</param>
    /// <returns>A memory stream containing the written bytes.</returns>
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
