// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

namespace RemoteMaster.Shared.Dtos;

public class MouseClickDto
{
    public long Button { get; init; }

    public bool Pressed { get; init; }

    public double X { get; init; }

    public double Y { get; init; }
}
