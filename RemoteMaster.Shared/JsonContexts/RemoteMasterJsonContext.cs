// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Text.Json.Serialization;
using RemoteMaster.Shared.DTOs;
using RemoteMaster.Shared.Models;

namespace RemoteMaster.Shared.JsonContexts;

[JsonSerializable(typeof(HostConfiguration))]
[JsonSerializable(typeof(SubjectDto))]
[JsonSerializable(typeof(HostDto))]
public partial class RemoteMasterJsonContext : JsonSerializerContext;
