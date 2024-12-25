// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using RemoteMaster.Host.Core.Abstractions;

namespace RemoteMaster.Host.Core.Models;

public class CapturingContext : ICapturingContext
{
    public event EventHandler? OnIsCursorVisibleChanged;

    public IScreen? SelectedScreen { get; set; }

    public string SelectedCodec { get; set; } = string.Empty;

    public int ImageQuality { get; set; }

    public int FrameRate { get; set; } = 60;

    public bool IsCursorVisible
    {
        get => _isCursorVisible;
        set
        {
            if (_isCursorVisible == value)
            {
                return;
            }

            _isCursorVisible = value;

            OnIsCursorVisibleChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    private bool _isCursorVisible;
}
