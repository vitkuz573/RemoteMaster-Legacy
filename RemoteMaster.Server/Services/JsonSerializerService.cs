// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using RemoteMaster.Server.Abstractions;

namespace RemoteMaster.Server.Services;

public class JsonSerializerService : ISerializationService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public byte[] SerializeToJsonBytes<T>(T obj)
    {
        try
        {
            var json = JsonSerializer.Serialize(obj, JsonOptions);

            return Encoding.UTF8.GetBytes(json);
        }
        catch (JsonException ex)
        {
            throw new ApplicationException("Error during JSON serialization.", ex);
        }
    }
}