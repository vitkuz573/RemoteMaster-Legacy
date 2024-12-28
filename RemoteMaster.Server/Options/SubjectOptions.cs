// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Text.Json.Serialization;

namespace RemoteMaster.Server.Options;

public class SubjectOptions
{
    [JsonPropertyName("organization")]
    public string Organization { get; set; } = string.Empty;

#pragma warning disable CA2227
    [JsonPropertyName("organizationalUnit")]
    public List<string> OrganizationalUnit { get; set; } = [];
#pragma warning restore CA2227

    [JsonPropertyName("locality")]
    public string Locality { get; set; } = string.Empty;

    [JsonPropertyName("state")]
    public string State { get; set; } = string.Empty;

    [JsonPropertyName("country")]
    public string Country { get; set; } = string.Empty;
}
