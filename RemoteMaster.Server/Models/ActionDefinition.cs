// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace RemoteMaster.Server.Models;

public class ActionDefinition
{
    public string Label { get; init; } = string.Empty;

    public EventCallback<MouseEventArgs> OnClick { get; init; }

    public Func<bool> IsVisible { get; init; } = () => true;

    public Func<bool> IsDisabled { get; init; } = () => false;

    public string Class { get; init; } = string.Empty;
}
