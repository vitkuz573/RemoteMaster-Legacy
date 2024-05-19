// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using Microsoft.AspNetCore.Components.Web;
using RemoteMaster.Server.Components.Library.Abstractions;

namespace RemoteMaster.Server.Components.Library.Models;

public class ActivatableCallback : IActivatable
{
    public Action<object, MouseEventArgs> ActivateCallback { get; set; }

    public void Activate(object sender, MouseEventArgs args)
    {
        ActivateCallback?.Invoke(sender, args);
    }
}