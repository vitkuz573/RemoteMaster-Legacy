// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Text.Json.Serialization;
using RemoteMaster.Host.Core.Models;

namespace RemoteMaster.Host.Core.JsonContexts;

[JsonSerializable(typeof(ModuleInfo))]
public partial class ModuleInfoJsonSerializerContext : JsonSerializerContext;
