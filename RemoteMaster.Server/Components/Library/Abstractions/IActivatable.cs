// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using Microsoft.AspNetCore.Components.Web;

namespace RemoteMaster.Server.Components.Library.Abstractions;

public interface IActivatable
{
    void Activate(object activator, MouseEventArgs args);
}
