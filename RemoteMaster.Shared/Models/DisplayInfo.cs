// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// Unauthorized copying of this file, via any medium is strictly prohibited.

using System.Drawing;

namespace RemoteMaster.Shared.Models;

public class DisplayInfo
{
    public string Name { get; set; }

    public bool IsPrimary { get; set; }

    public Size Resolution { get; set; }
}
