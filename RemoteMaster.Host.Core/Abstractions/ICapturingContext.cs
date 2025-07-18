﻿// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

namespace RemoteMaster.Host.Core.Abstractions;

public interface ICapturingContext
{
    event EventHandler? OnIsCursorVisibleChanged;

    IScreen? SelectedScreen { get; set; }
    
    string SelectedCodec { get; set; }

    int ImageQuality { get; set; }

    int FrameRate { get; set; }

    bool IsCursorVisible { get; set; }
}
