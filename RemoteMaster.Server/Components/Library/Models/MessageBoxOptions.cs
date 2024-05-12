// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using Microsoft.AspNetCore.Components;

namespace RemoteMaster.Server.Components.Library.Models;

public class MessageBoxOptions
{
    public string Title { get; set; }

    public string Message { get; set; }

    public MarkupString MarkupMessage { get; set; }

    public string YesText { get; set; } = "OK";

    public string NoText { get; set; }

    public string CancelText { get; set; }
}