// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Text.Json.Serialization;
using RemoteMaster.Shared.Models;

namespace RemoteMaster.Server.Options;

public class CertificateOptions
{
    [JsonPropertyName("keySize")]
    public int KeySize { get; set; }

    [JsonPropertyName("validityPeriod")]
    public int ValidityPeriod { get; set; }

    [JsonPropertyName("commonName")]
    public string CommonName { get; set; }

    [JsonPropertyName("subject")]
    public SubjectOptions Subject { get; set; }
}
