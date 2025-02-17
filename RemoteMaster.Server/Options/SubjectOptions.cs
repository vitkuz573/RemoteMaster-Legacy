// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using RemoteMaster.Server.Attributes;

namespace RemoteMaster.Server.Options;

public class SubjectOptions
{
    [JsonPropertyName("organization")]
    [Required(ErrorMessage = "Organization is required.")]
    public string Organization { get; set; } = string.Empty;

    [JsonPropertyName("organizationalUnit")]
    [CustomMinLength(1, ErrorMessage = "At least one OrganizationalUnit is required.")]
    public List<string> OrganizationalUnit { get; } = [];

    [JsonPropertyName("locality")]
    [Required(ErrorMessage = "Locality is required.")]
    public string Locality { get; set; } = string.Empty;

    [JsonPropertyName("state")]
    [Required(ErrorMessage = "State is required.")]
    public string State { get; set; } = string.Empty;

    [JsonPropertyName("country")]
    [Required(ErrorMessage = "Country is required.")]
    public string Country { get; set; } = string.Empty;
}
