// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using RemoteMaster.Shared.Dtos;

namespace RemoteMaster.Host.Core.Abstractions;

public interface IInputService : IDisposable
{
    bool InputEnabled { get; set; }

    void SendMouseInput(MouseInputDto dto, IScreenCapturerService screenCapturer);

    void SendKeyboardInput(KeyboardInputDto dto);
}