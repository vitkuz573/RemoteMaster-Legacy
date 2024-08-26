// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.ComponentModel.DataAnnotations;
using System.Net;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Identity;
using RemoteMaster.Server.Enums;
using Serilog;

namespace RemoteMaster.Server.Components.Account.Pages;

public partial class Login
{
    private string? _errorMessage;

    [CascadingParameter]
    private HttpContext HttpContext { get; set; } = default!;

    [SupplyParameterFromForm]
    private InputModel Input { get; set; } = new();

    [SupplyParameterFromQuery]
    private string? ReturnUrl { get; set; }

    protected async override Task OnInitializedAsync()
    {
        if (HttpMethods.IsGet(HttpContext.Request.Method))
        {
            await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);
        }

        if (HttpContext.User.Identity?.IsAuthenticated == true)
        {
            Log.Information("User already logged in. Redirecting to the origin page or default page.");
            RedirectManager.RedirectTo(ReturnUrl ?? "/");

            return;
        }
    }

    public async Task LoginUser()
    {
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var user = await UserManager.FindByNameAsync(Input.Username);

        if (user == null)
        {
            _errorMessage = "Error: Invalid login attempt.";
            return;
        }

        var userRoles = await UserManager.GetRolesAsync(user);

        if (!userRoles.Any())
        {
            _errorMessage = "Error: User does not belong to any roles.";
            await ApplicationUserService.AddSignInEntry(user, false);
            return;
        }

        var isRootAdmin = await UserManager.IsInRoleAsync(user, "RootAdministrator");
        var isLocalhost = IPAddress.IsLoopback(IPAddress.Parse(ipAddress));

        if (isRootAdmin && !isLocalhost)
        {
            Log.Warning("Attempt to login as RootAdministrator from non-localhost IP.");
            _errorMessage = "Error: RootAdministrator access is restricted to localhost.";
            await ApplicationUserService.AddSignInEntry(user, false);
            return;
        }

        var result = await SignInManager.PasswordSignInAsync(Input.Username, Input.Password, false, false);

        if (result.Succeeded)
        {
            if (isRootAdmin && isLocalhost)
            {
                Log.Information("RootAdministrator logged in from localhost. Redirecting to Admin page.");
                await ApplicationUserService.AddSignInEntry(user, true);
                RedirectManager.RedirectTo("Admin");
                return;
            }

            await TokenService.RevokeAllRefreshTokensAsync(user.Id, TokenRevocationReason.PreemptiveRevocation);

            Log.Information("User logged in. All previous refresh tokens revoked.");

            var tokenDataResult = await TokenService.GenerateTokensAsync(user.Id);

            if (tokenDataResult.IsSuccess)
            {
                var storeTokensResult = await TokenStorageService.StoreTokensAsync(user.Id, tokenDataResult.Value);

                if (storeTokensResult.IsSuccess)
                {
                    Log.Information("User {Username} logged in from IP {IPAddress} at {LoginTime}.", Input.Username, ipAddress, DateTime.UtcNow.ToLocalTime());
                    await ApplicationUserService.AddSignInEntry(user, true);
                    RedirectManager.RedirectTo(ReturnUrl);
                }
                else
                {
                    _errorMessage = "Error: Failed to store tokens.";
                    await ApplicationUserService.AddSignInEntry(user, false);
                }
            }
            else
            {
                _errorMessage = "Error: Failed to generate tokens.";
                await ApplicationUserService.AddSignInEntry(user, false);
            }
        }
        else if (result.RequiresTwoFactor)
        {
            RedirectManager.RedirectTo("Account/LoginWith2fa", new()
            {
                ["returnUrl"] = ReturnUrl
            });
        }
        else if (result.IsLockedOut)
        {
            Log.Warning("User account locked out.");
            await ApplicationUserService.AddSignInEntry(user, false);
            RedirectManager.RedirectTo("Account/Lockout");
        }
        else
        {
            _errorMessage = "Error: Invalid login attempt.";
            await ApplicationUserService.AddSignInEntry(user, false);
        }
    }

    private sealed class InputModel
    {
        [Required]
        [DataType(DataType.Text)]
        public string Username { get; set; } = "";

        [Required]
        [DataType(DataType.Password)]
        public string Password { get; set; } = "";
    }
}
