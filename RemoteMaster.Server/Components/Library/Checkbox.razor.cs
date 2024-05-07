// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using Microsoft.AspNetCore.Components;

namespace RemoteMaster.Server.Components.Library;

public partial class Checkbox
{
    [Parameter]
    public string Label { get; set; }

    [Parameter]
    public bool IsChecked { get; set; }

    [Parameter]
    public EventCallback<bool> IsCheckedChanged { get; set; }
}
