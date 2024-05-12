// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

namespace RemoteMaster.Server.Components.Library.Models;

public class CssStyleCondition(string property, string value, bool condition)
{
    public string Property { get; } = property;

    public string Value { get; } = value;

    public bool Condition { get; set; } = condition;
}
