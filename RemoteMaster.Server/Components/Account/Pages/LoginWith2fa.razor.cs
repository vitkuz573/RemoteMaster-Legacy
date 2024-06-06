// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Components;
using RemoteMaster.Server.Data;
using Serilog;

namespace RemoteMaster.Server.Components.Account.Pages;

public partial class LoginWith2fa
{
    private string? message;
    private ApplicationUser user = default!;

    [SupplyParameterFromForm]
    private InputModel Input { get; set; } = new();

    [SupplyParameterFromQuery]
    private string? ReturnUrl { get; set; }

    [SupplyParameterFromQuery]
    private bool RememberMe { get; set; }

    protected async override Task OnInitializedAsync()
    {
        user = await SignInManager.GetTwoFactorAuthenticationUserAsync() ?? throw new InvalidOperationException("Unable to load two-factor authentication user.");
    }

    private async Task OnValidSubmitAsync()
    {
        var authenticatorCode = Input.TwoFactorCode!.Replace(" ", string.Empty).Replace("-", string.Empty);
        var result = await SignInManager.TwoFactorAuthenticatorSignInAsync(authenticatorCode, RememberMe, false);
        var userId = await UserManager.GetUserIdAsync(user);

        if (result.Succeeded)
        {
            Log.Information("User with ID '{UserId}' logged in with 2fa.", userId);
            RedirectManager.RedirectTo(ReturnUrl);
        }
        else if (result.IsLockedOut)
        {
            Log.Warning("User with ID '{UserId}' account locked out.", userId);
            RedirectManager.RedirectTo("Account/Lockout");
        }
        else
        {
            Log.Warning("Invalid authenticator code entered for user with ID '{UserId}'.", userId);
            message = "Error: Invalid authenticator code.";
        }
    }

    private sealed class InputModel
    {
        [Required]
        [StringLength(7, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 6)]
        [DataType(DataType.Text)]
        [Display(Name = "Authenticator code")]
        public string? TwoFactorCode { get; set; }
    }
}
