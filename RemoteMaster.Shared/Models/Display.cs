// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Drawing;

namespace RemoteMaster.Shared.Models;

public class Display
{
    public required string Name { get; set; }

    public required bool IsPrimary { get; set; }

    public required Size Resolution { get; set; }
}
