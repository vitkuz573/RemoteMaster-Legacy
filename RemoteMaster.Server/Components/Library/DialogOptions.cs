// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

namespace RemoteMaster.Server.Components.Library;

public class DialogOptions
{
    public bool? BackdropClick { get; set; }

    public bool? CloseOnEscapeKey { get; set; }

    public bool? NoHeader { get; set; }

    public bool? CloseButton { get; set; }

    public bool? FullScreen { get; set; }

    public bool? FullWidth { get; set; }

    public string? BackgroundClass { get; set; }
}