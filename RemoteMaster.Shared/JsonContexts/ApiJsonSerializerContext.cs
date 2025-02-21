// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Text.Json.Serialization;
using RemoteMaster.Shared.Converters;
using RemoteMaster.Shared.DTOs;
using RemoteMaster.Shared.Models;

namespace RemoteMaster.Shared.JsonContexts;

[JsonSourceGenerationOptions(DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull, PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase, Converters = [typeof(IPAddressConverter), typeof(PhysicalAddressConverter)])]
[JsonSerializable(typeof(ApiResponse))]
[JsonSerializable(typeof(ApiResponse<byte[]>))]
[JsonSerializable(typeof(ApiResponse<OrganizationDto>))]
[JsonSerializable(typeof(ApiResponse<HostMoveRequest>))]
[JsonSerializable(typeof(ApiResponse<List<HealthCheck>>))]
public partial class ApiJsonSerializerContext : JsonSerializerContext;
