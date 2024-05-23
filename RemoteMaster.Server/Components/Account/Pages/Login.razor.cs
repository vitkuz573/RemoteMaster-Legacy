// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Identity;
using RemoteMaster.Server.Enums;
using Serilog;

namespace RemoteMaster.Server.Components.Account.Pages;

public partial class Login
{
    private string? errorMessage;

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
        var result = await SignInManager.PasswordSignInAsync(Input.Username, Input.Password, false, false);

        if (result.Succeeded)
        {
            var user = await UserManager.FindByNameAsync(Input.Username);

            if (user == null)
            {
                errorMessage = "Error: Invalid login attempt.";
                return;
            }

            var userId = user.Id;
            var isRootAdmin = await UserManager.IsInRoleAsync(user, "RootAdministrator");
            var isLocalhost = ipAddress == "127.0.0.1" || ipAddress == "::1" || ipAddress == "::ffff:127.0.0.1";

            if (isRootAdmin && !isLocalhost)
            {
                await SignInManager.SignOutAsync();
                Log.Warning("Attempt to login as RootAdministrator from non-localhost IP.");
                errorMessage = "RootAdministrator access is restricted to localhost.";
                return;
            }

            if (isRootAdmin && isLocalhost)
            {
                Log.Information("RootAdministrator logged in from localhost. Redirecting to Admin page.");
                RedirectManager.RedirectTo("Admin");
                return;
            }

            await TokenService.RevokeAllRefreshTokensAsync(userId, TokenRevocationReason.PreemptiveSecurity);

            Log.Information("User logged in. All previous refresh tokens revoked.");

            var claims = new List<Claim>
            {
                new(ClaimTypes.Name, user.UserName),
                new(ClaimTypes.NameIdentifier, user.Id.ToString())
            };

            var userRoles = await UserManager.GetRolesAsync(user);

            foreach (var role in userRoles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }

            var accessToken = await TokenService.GenerateAccessTokenAsync(claims);
            var refreshToken = TokenService.GenerateRefreshToken(userId, ipAddress);

            await TokenStorageService.StoreTokensAsync(userId, accessToken, refreshToken);

            RedirectManager.RedirectTo(ReturnUrl);
        }
        else
        {
            errorMessage = "Error: Invalid login attempt.";
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
