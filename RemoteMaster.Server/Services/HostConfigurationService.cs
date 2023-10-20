// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using RemoteMaster.Server.Abstractions;
using RemoteMaster.Shared.Models;

namespace RemoteMaster.Server.Services;

/// <summary>
/// Service responsible for generating host configuration files.
/// </summary>
public class HostConfigurationService : IHostConfigurationService
{
    private readonly ISerializationService _serializationService;
    private readonly ILogger<HostConfigurationService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="HostConfigurationService"/> class.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="serializationService">The serialization service instance.</param>
    public HostConfigurationService(ISerializationService serializationService, ILogger<HostConfigurationService> logger)
    {
        _serializationService = serializationService;
        _logger = logger;
    }

    /// <summary>
    /// Generates a configuration file for the host based on the given configuration model.
    /// </summary>
    /// <param name="config">The configuration model.</param>
    /// <returns>A memory stream containing the configuration file.</returns>
    public async Task<MemoryStream> GenerateConfigFileAsync(HostConfiguration config)
    {
        var jsonBytes = _serializationService.SerializeToJsonBytes(config);
        
        return await WriteToMemoryStreamAsync(jsonBytes);
    }

    /// <summary>
    /// Writes the given bytes to a memory stream.
    /// </summary>
    /// <param name="bytes">The bytes to write.</param>
    /// <returns>A memory stream containing the written bytes.</returns>
    private async Task<MemoryStream> WriteToMemoryStreamAsync(byte[] bytes)
    {
        var memoryStream = new MemoryStream();

        try
        {
            await memoryStream.WriteAsync(bytes);
            _logger.LogInformation("Successfully generated config file.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while writing to the memory stream.");
            throw;
        }

        return memoryStream;
    }
}
