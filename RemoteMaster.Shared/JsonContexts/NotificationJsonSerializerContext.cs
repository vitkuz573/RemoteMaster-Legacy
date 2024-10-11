// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Text.Json.Serialization;
using RemoteMaster.Shared.Models;

namespace RemoteMaster.Shared.JsonContexts;

[JsonSerializable(typeof(NotificationMessage))]
public partial class NotificationJsonSerializerContext : JsonSerializerContext;
