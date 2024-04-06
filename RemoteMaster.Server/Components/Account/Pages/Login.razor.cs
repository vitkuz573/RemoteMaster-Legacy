// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Identity;

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
    }

    public async Task LoginUser()
    {
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();

        var result = await SignInManager.PasswordSignInAsync(Input.Email, Input.Password, Input.RememberMe, lockoutOnFailure: false);

        if (result.Succeeded)
        {
            var userId = UserManager.GetUserId(HttpContext.User);

            Logger.LogInformation("User logged in.");

            var accessTokenString = TokenService.GenerateAccessToken(Input.Email);
            var refreshTokenString = TokenService.GenerateRefreshToken(userId, ipAddress);

            SetTokenCookies(accessTokenString, refreshTokenString);

            RedirectManager.RedirectTo(ReturnUrl);
        }
        else if (result.RequiresTwoFactor)
        {
            RedirectManager.RedirectTo("Account/LoginWith2fa", new()
            {
                ["returnUrl"] = ReturnUrl,
                ["rememberMe"] = Input.RememberMe
            });
        }
        else if (result.IsLockedOut)
        {
            Logger.LogWarning("User account locked out.");
            RedirectManager.RedirectTo("Account/Lockout");
        }
        else
        {
            errorMessage = "Error: Invalid login attempt.";
        }
    }

    private void SetTokenCookies(string accessToken, string refreshToken)
    {
        var baseCookieOptions = new CookieOptions
        {
            HttpOnly = true,
            Secure = false,
            SameSite = SameSiteMode.Strict
        };

        var accessTokenOptions = CopyCookieOptions(baseCookieOptions);
        accessTokenOptions.Expires = DateTime.UtcNow.AddSeconds(10);

        HttpContext.Response.Cookies.Append("accessToken", accessToken, accessTokenOptions);

        var refreshTokenOptions = CopyCookieOptions(baseCookieOptions);
        refreshTokenOptions.Expires = DateTime.UtcNow.AddDays(7);

        HttpContext.Response.Cookies.Append("refreshToken", refreshToken, refreshTokenOptions);
    }

    private static CookieOptions CopyCookieOptions(CookieOptions options)
    {
        return new CookieOptions
        {
            HttpOnly = options.HttpOnly,
            Secure = options.Secure,
            SameSite = options.SameSite,
            Expires = options.Expires
        };
    }

    private sealed class InputModel
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = "";

        [Required]
        [DataType(DataType.Password)]
        public string Password { get; set; } = "";

        [Display(Name = "Remember me?")]
        public bool RememberMe { get; set; }
    }
}
