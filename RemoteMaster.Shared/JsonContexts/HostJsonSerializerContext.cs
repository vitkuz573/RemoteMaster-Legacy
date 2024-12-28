// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Text.Json.Serialization;
using RemoteMaster.Shared.Converters;
using RemoteMaster.Shared.Models;

namespace RemoteMaster.Shared.JsonContexts;

[JsonSourceGenerationOptions(WriteIndented = true, DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull, PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase, Converters = [typeof(IPAddressConverter), typeof(PhysicalAddressConverter)])]
[JsonSerializable(typeof(List<HostMoveRequest>))]
[JsonSerializable(typeof(HostRegisterRequest))]
[JsonSerializable(typeof(HostUnregisterRequest))]
[JsonSerializable(typeof(HostUpdateRequest))]
public partial class HostJsonSerializerContext : JsonSerializerContext;
