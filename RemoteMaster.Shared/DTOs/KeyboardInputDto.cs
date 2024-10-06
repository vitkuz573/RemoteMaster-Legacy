// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

namespace RemoteMaster.Shared.DTOs;

public class KeyboardInputDto(string code, bool isPressed)
{
    public string Code { get; } = code;

    public bool IsPressed { get; } = isPressed;
}
