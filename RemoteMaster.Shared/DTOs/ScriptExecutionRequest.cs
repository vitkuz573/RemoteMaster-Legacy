﻿// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using RemoteMaster.Shared.Enums;

namespace RemoteMaster.Shared.DTOs;

public class ScriptExecutionRequest(string content, Shell shell)
{
    public string Content { get; } = content;

    public Shell Shell { get; } = shell;

    public bool AsSystem { get; init; }
}
