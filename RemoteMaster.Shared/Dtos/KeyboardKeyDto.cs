// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// Unauthorized copying of this file, via any medium is strictly prohibited.

using RemoteMaster.Shared.Models;

namespace RemoteMaster.Shared.Dtos;

public class KeyboardKeyDto
{
    public int Key { get; set; }

    public ButtonAction State { get; set; }
}
