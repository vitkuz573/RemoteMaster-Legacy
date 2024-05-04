// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Diagnostics;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Diagnostics;

namespace RemoteMaster.Server.Components.Pages;

public partial class Error
{
    [CascadingParameter]
    private HttpContext? HttpContext { get; set; }

    private string? RequestId { get; set; }

    private bool ShowRequestId => !string.IsNullOrEmpty(RequestId);

    private IExceptionHandlerFeature? ExceptionDetails { get; set; }

    protected override void OnInitialized()
    {
        RequestId = Activity.Current?.Id ?? HttpContext?.TraceIdentifier;

        if (HttpContext != null)
        {
            ExceptionDetails = HttpContext.Features.Get<IExceptionHandlerFeature>();
        }
    }
}

