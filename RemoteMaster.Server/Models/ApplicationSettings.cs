// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Text.Json.Serialization;

namespace RemoteMaster.Server.Models;

public class ApplicationSettings
{
    [JsonPropertyName("executablesRoot")]
    public string ExecutablesRoot { get; init; }

    [JsonPropertyName("isRegisterAllowed")]
    public bool IsRegisterAllowed { get; init; }

    [JsonPropertyName("jwt")]
    public JwtOptions Jwt { get; init; }

    [JsonPropertyName("caSettings")]
    public CertificateOptions CASettings { get; init; }
}

