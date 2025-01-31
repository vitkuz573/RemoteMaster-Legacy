// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using RemoteMaster.Host.Core.Abstractions;
using RemoteMaster.Shared.DTOs;

namespace RemoteMaster.Host.Linux.Services;

public class InputService : IInputService
{
    private readonly CancellationTokenSource _cts = new();
    private bool _disposed;

    public bool InputEnabled { get; set; }

    public bool BlockUserInput { get; set; }

    public void Start()
    {
        ObjectDisposedException.ThrowIf(_disposed, nameof(InputService));
    }

    public void HandleMouseInput(MouseInputDto dto, string connectionId) => throw new NotImplementedException();

    public void HandleKeyboardInput(KeyboardInputDto dto, string connectionId) => throw new NotImplementedException();

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    private void Dispose(bool disposing)
    {
        if (_disposed)
        {
            return;
        }

        if (!disposing)
        {
            return;
        }

        _cts.Cancel();

        _cts.Dispose();

        _disposed = true;
    }

    ~InputService()
    {
        Dispose(false);
    }
}
