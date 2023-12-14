// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.ComponentModel.DataAnnotations.Schema;
using System.Net.NetworkInformation;
using System.Text.Json.Serialization;

namespace RemoteMaster.Shared.Models;

public class Computer : Node
{
    [JsonPropertyName("ipAddress")]
    public required string IPAddress { get; set; }

    [JsonPropertyName("macAddress")]
    public required string MACAddress { get; set; }

    [JsonIgnore]
    [NotMapped]
    public byte[]? Thumbnail { get; set; }

    public async Task<bool> IsAvailable()
    {
        try
        {
            using var ping = new Ping();
            var reply = await ping.SendPingAsync(IPAddress, 1000);

            return reply.Status == IPStatus.Success;
        }
        catch
        {
            return false;
        }
    }
}
