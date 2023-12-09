// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

namespace RemoteMaster.Shared.Dtos;

public class MouseClickDto
{
    public long Button { get; set; }

    public bool Pressed { get; set; }

    public double X { get; set; }

    public double Y { get; set; }
}
