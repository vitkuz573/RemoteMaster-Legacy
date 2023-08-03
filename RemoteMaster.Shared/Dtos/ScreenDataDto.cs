// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Drawing;
using RemoteMaster.Shared.Models;

namespace RemoteMaster.Shared.Dtos;

public class ScreenDataDto
{
    public IEnumerable<DisplayInfo> Displays { get; init; } = Enumerable.Empty<DisplayInfo>();

    public Size ScreenSize { get; set; }
}
