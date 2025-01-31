// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using RemoteMaster.Host.Core.Abstractions;
using RemoteMaster.Shared.DTOs;

namespace RemoteMaster.Host.Linux.Services;

public class InputService : IInputService
{
    public bool InputEnabled { get; set; }

    public bool BlockUserInput { get; set; }

    public void Start() => throw new NotImplementedException();

    public void HandleMouseInput(MouseInputDto dto, string connectionId) => throw new NotImplementedException();

    public void HandleKeyboardInput(KeyboardInputDto dto, string connectionId) => throw new NotImplementedException();

    public void Dispose() => throw new NotImplementedException();
}
