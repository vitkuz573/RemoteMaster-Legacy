// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Components;
using RemoteMaster.Server.Aggregates.ApplicationUserAggregate;
using RemoteMaster.Server.Entities;
using Serilog;

namespace RemoteMaster.Server.Components.Account.Pages;

public partial class LoginWith2fa
{
    private string? _message;
    private ApplicationUser _user = default!;

    [CascadingParameter]
    private HttpContext HttpContext { get; set; } = default!;

    [SupplyParameterFromForm]
    private InputModel Input { get; set; } = new();

    [SupplyParameterFromQuery]
    private string? ReturnUrl { get; set; }

    [SupplyParameterFromQuery]
    private bool RememberMe { get; set; }

    protected async override Task OnInitializedAsync()
    {
        _user = await SignInManager.GetTwoFactorAuthenticationUserAsync() ?? throw new InvalidOperationException("Unable to load two-factor authentication user.");
    }

    private async Task OnValidSubmitAsync()
    {
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var authenticatorCode = Input.TwoFactorCode!.Replace(" ", string.Empty).Replace("-", string.Empty);
        var result = await SignInManager.TwoFactorAuthenticatorSignInAsync(authenticatorCode, RememberMe, false);
        var userId = await UserManager.GetUserIdAsync(_user);

        var userRoles = await UserManager.GetRolesAsync(_user);

        if (!userRoles.Any())
        {
            _message = "Error: User does not belong to any roles.";
            await ApplicationUserService.AddSignInEntry(_user, false);
            return;
        }

        if (result.Succeeded)
        {
            Log.Information("User with ID '{UserId}' logged in with 2fa.", userId);

            var tokenDataResult = await TokenService.GenerateTokensAsync(userId);

            if (tokenDataResult.IsSuccess)
            {
                var storeTokensResult = await TokenStorageService.StoreTokensAsync(userId, tokenDataResult.Value);

                if (storeTokensResult.IsSuccess)
                {
                    await ApplicationUserService.AddSignInEntry(_user, true);
                    RedirectManager.RedirectTo(ReturnUrl);
                }
                else
                {
                    _message = "Error: Failed to store tokens.";
                    await ApplicationUserService.AddSignInEntry(_user, false);
                }
            }
            else
            {
                _message = "Error: Failed to generate tokens.";
                await ApplicationUserService.AddSignInEntry(_user, false);
            }
        }
        else if (result.IsLockedOut)
        {
            Log.Warning("User with ID '{UserId}' account locked out.", userId);
            await ApplicationUserService.AddSignInEntry(_user, false);
            RedirectManager.RedirectTo("Account/Lockout");
        }
        else
        {
            Log.Warning("Invalid authenticator code entered for user with ID '{UserId}'.", userId);
            _message = "Error: Invalid authenticator code.";
            await ApplicationUserService.AddSignInEntry(_user, false);
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
