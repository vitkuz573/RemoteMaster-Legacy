// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using Microsoft.AspNetCore.Components;

namespace RemoteMaster.Server.Components.Account.Shared;

public partial class StatusMessage
{
    private string? _messageFromCookie;

    [Parameter]
    public string? Message { get; set; }

    [Parameter]
    public bool UseHttpContext { get; set; } = true;

    [CascadingParameter]
    private HttpContext HttpContext { get; set; } = default!;

    private string? DisplayMessage => Message ?? _messageFromCookie;

    protected override void OnInitialized()
    {
        if (!UseHttpContext)
        {
            return;
        }

        _messageFromCookie = HttpContext.Request.Cookies[IdentityRedirectManager.StatusCookieName];

        if (_messageFromCookie is not null)
        {
            HttpContext.Response.Cookies.Delete(IdentityRedirectManager.StatusCookieName);
        }
    }
}
