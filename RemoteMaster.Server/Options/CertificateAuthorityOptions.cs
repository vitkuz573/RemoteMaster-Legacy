// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Text.Json.Serialization;
using RemoteMaster.Server.Enums;

namespace RemoteMaster.Server.Options;

public class CertificateAuthorityOptions
{
    [JsonPropertyName("type")]
    public CaType Type { get; set; }

    [JsonPropertyName("internalOptions")]
    public InternalCertificateOptions? InternalOptions { get; set; }

    [JsonPropertyName("activeDirectoryOptions")]
    public ActiveDirectoryOptions? ActiveDirectoryOptions { get; set; }
}
