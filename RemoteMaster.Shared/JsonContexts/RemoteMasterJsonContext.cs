// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using RemoteMaster.Shared.DTOs;
using RemoteMaster.Shared.Models;
using System.Text.Json.Serialization;

namespace RemoteMaster.Shared.JsonContexts;

[JsonSerializable(typeof(HostConfiguration))]
[JsonSerializable(typeof(SubjectDto))]
[JsonSerializable(typeof(HostDto))]
public partial class RemoteMasterJsonContext : JsonSerializerContext
{
}
