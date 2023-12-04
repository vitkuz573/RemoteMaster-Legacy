// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Text.Json.Serialization;

namespace RemoteMaster.Host.Core.Models;

public class Subject
{
    [JsonPropertyName("organization")]
    public string Organization { get; set; }

    [JsonPropertyName("locality")]
    public string Locality { get; set; }

    [JsonPropertyName("state")]
    public string State { get; set; }

    [JsonPropertyName("Country")]
    public string Country { get; set; }
}
