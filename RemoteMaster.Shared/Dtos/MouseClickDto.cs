// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using RemoteMaster.Shared.Models;

namespace RemoteMaster.Shared.Dtos;

public class MouseClickDto
{
    public long Button { get; set; }

    public ButtonState State { get; set; }

    public double X { get; set; }

    public double Y { get; set; }
}
