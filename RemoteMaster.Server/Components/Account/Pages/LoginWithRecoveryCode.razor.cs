﻿// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Components;
using RemoteMaster.Server.Aggregates.ApplicationUserAggregate;

namespace RemoteMaster.Server.Components.Account.Pages;

public partial class LoginWithRecoveryCode
{
    private string? _message;
    private ApplicationUser _user = default!;

    [SupplyParameterFromForm]
    private InputModel Input { get; set; } = new();

    [SupplyParameterFromQuery]
    private string? ReturnUrl { get; set; }

    protected async override Task OnInitializedAsync()
    {
        _user = await SignInManager.GetTwoFactorAuthenticationUserAsync() ?? throw new InvalidOperationException("Unable to load two-factor authentication user.");
    }

    private async Task OnValidSubmitAsync()
    {
        var recoveryCode = Input.RecoveryCode.Replace(" ", string.Empty);

        var result = await SignInManager.TwoFactorRecoveryCodeSignInAsync(recoveryCode);

        var userId = await UserManager.GetUserIdAsync(_user);

        var userRoles = await UserManager.GetRolesAsync(_user);

        if (!userRoles.Any())
        {
            _message = "Error: User does not belong to any roles.";
            await ApplicationUserService.AddSignInEntryAsync(_user, false);
            return;
        }

        if (result.Succeeded)
        {
            Logger.LogInformation("User with ID '{UserId}' logged in with a recovery code.", userId);

            var tokenDataResult = await TokenService.GenerateTokensAsync(userId);

            if (tokenDataResult.IsSuccess)
            {
                var storeTokensResult = await TokenStorageService.StoreTokensAsync(userId, tokenDataResult.Value);

                if (storeTokensResult.IsSuccess)
                {
                    await ApplicationUserService.AddSignInEntryAsync(_user, true);
                    RedirectManager.RedirectTo(ReturnUrl);
                }
                else
                {
                    _message = "Error: Failed to store tokens.";
                    await ApplicationUserService.AddSignInEntryAsync(_user, false);
                }
            }
            else
            {
                _message = "Error: Failed to generate tokens.";
                await ApplicationUserService.AddSignInEntryAsync(_user, false);
            }
        }
        else if (result.IsLockedOut)
        {
            Logger.LogWarning("User with ID '{UserId}' account locked out.", userId);
            _message = "Error: Your account has been locked out.";
            await ApplicationUserService.AddSignInEntryAsync(_user, false);
        }
        else
        {
            Logger.LogWarning("Invalid recovery code entered for user with ID '{UserId}'.", userId);
            _message = "Error: Invalid recovery code entered.";
            await ApplicationUserService.AddSignInEntryAsync(_user, false);
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
