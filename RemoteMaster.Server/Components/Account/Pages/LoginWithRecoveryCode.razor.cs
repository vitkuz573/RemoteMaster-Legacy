// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Components;
using RemoteMaster.Server.Data;

namespace RemoteMaster.Server.Components.Account.Pages;

public partial class LoginWithRecoveryCode
{
    private string? message;
    private ApplicationUser user = default!;

    [SupplyParameterFromForm]
    private InputModel Input { get; set; } = new();

    [SupplyParameterFromQuery]
    private string? ReturnUrl { get; set; }

    protected async override Task OnInitializedAsync()
    {
        user = await SignInManager.GetTwoFactorAuthenticationUserAsync() ?? throw new InvalidOperationException("Unable to load two-factor authentication user.");
    }

    private async Task OnValidSubmitAsync()
    {
        var recoveryCode = Input.RecoveryCode.Replace(" ", string.Empty);
        var result = await SignInManager.TwoFactorRecoveryCodeSignInAsync(recoveryCode);
        var userId = await UserManager.GetUserIdAsync(user);

        if (result.Succeeded)
        {
            Logger.LogInformation("User with ID '{UserId}' logged in with a recovery code.", userId);
            RedirectManager.RedirectTo(ReturnUrl);
        }
        else
        {
            Logger.LogWarning("Invalid recovery code entered for user with ID '{UserId}' ", userId);
            message = "Error: Invalid recovery code entered.";
        }
    }

    private sealed class InputModel
    {
        [Required]
        [DataType(DataType.Text)]
        [Display(Name = "Recovery Code")]
        public string RecoveryCode { get; set; } = "";
    }
}
