// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using RemoteMaster.Shared.DTOs;

namespace RemoteMaster.Host.Core.Abstractions;

public interface IInputService : IDisposable
{
    bool InputEnabled { get; set; }

    bool BlockUserInput { get; set; }

    void Start();

    void HandleMouseInput(MouseInputDto dto, IScreenCapturingService screenCapturing);

    void HandleKeyboardInput(KeyboardInputDto dto);
}