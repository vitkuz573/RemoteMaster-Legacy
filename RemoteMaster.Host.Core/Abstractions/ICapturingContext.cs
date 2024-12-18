// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

namespace RemoteMaster.Host.Core.Abstractions;

public interface ICapturingContext : IDisposable
{
    event EventHandler? OnDrawCursorChanged;

    string ConnectionId { get; } 
    
    IScreen? SelectedScreen { get; set; }
    
    string SelectedCodec { get; set; }

    int ImageQuality { get; set; }

    int FrameRate { get; set; }

    bool DrawCursor { get; set; }

    CancellationTokenSource CancellationTokenSource { get; }
}
