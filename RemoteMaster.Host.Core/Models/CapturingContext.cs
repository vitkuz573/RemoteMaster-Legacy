// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using RemoteMaster.Host.Core.Abstractions;

namespace RemoteMaster.Host.Core.Models;

public class CapturingContext : ICapturingContext
{
    public event EventHandler? OnDrawCursorChanged;

    public IScreen? SelectedScreen { get; set; }

    public string SelectedCodec { get; set; } = string.Empty;

    public int ImageQuality { get; set; }

    public int FrameRate { get; set; } = 60;

    public bool DrawCursor
    {
        get => _drawCursor;
        set
        {
            if (_drawCursor == value)
            {
                return;
            }

            _drawCursor = value;

            OnDrawCursorChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    private bool _drawCursor;
}
