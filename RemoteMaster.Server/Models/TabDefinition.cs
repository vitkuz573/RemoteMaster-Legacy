// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

namespace RemoteMaster.Server.Models;

public class TabDefinition(string title, string icon)
{
    public string Title { get; } = title;

    public string Icon { get; } = icon;

    public List<ActionDefinition> Actions { get; } = [];
}
