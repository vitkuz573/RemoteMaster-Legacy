// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Text.Json.Serialization;

namespace RemoteMaster.Server.Options;

public class JwtOptions
{
    [JsonPropertyName("keysDirectory")]
    public string? KeysDirectory { get; set; }

    [JsonPropertyName("keySize")]
    public int? KeySize { get; set; }

    [JsonPropertyName("keyPassword")]
    public string? KeyPassword { get; set; }
}
