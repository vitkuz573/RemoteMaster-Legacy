// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Drawing;

namespace RemoteMaster.Shared.DTOs;

public class MouseInputDto
{
    public long? Button { get; set; }

    public bool? IsPressed { get; set; }

    public PointF? Position { get; init; }

    public double? DeltaY { get; init; }
}
