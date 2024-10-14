// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.ComponentModel.DataAnnotations;
using System.Net;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Identity;
using RemoteMaster.Server.Enums;

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
            Logger.LogInformation("User already logged in. Redirecting to the origin page or default page.");
            RedirectManager.RedirectTo(ReturnUrl ?? "/");
        }
    }

    public async Task LoginUser()
    {
        var ipAddress = HttpContext.Connection.RemoteIpAddress ?? IPAddress.None;
        var result = await AuthenticationService.LoginAsync(Input.Username, Input.Password, ipAddress, ReturnUrl);

        switch (result.Status)
        {
            case AuthenticationStatus.Success:
                RedirectManager.RedirectTo(result.RedirectUrl ?? "/");
                break;

            case AuthenticationStatus.RequiresTwoFactor:
                RedirectManager.RedirectTo("Account/LoginWith2fa", new()
                {
                    ["returnUrl"] = ReturnUrl
                });
                break;

            case AuthenticationStatus.LockedOut:
                _errorMessage = "Error: Your account has been locked out.";
                break;

            case AuthenticationStatus.InvalidCredentials:
                _errorMessage = "Error: Invalid login attempt.";
                break;

            case AuthenticationStatus.NoRolesAssigned:
                _errorMessage = "Error: User does not belong to any roles.";
                break;

            case AuthenticationStatus.RootAdminAccessDenied:
                _errorMessage = "Error: RootAdministrator access is restricted to localhost.";
                break;

            case AuthenticationStatus.TokenGenerationFailed:
                _errorMessage = "Error: Failed to generate tokens.";
                break;

            case AuthenticationStatus.TokenStorageFailed:
                _errorMessage = "Error: Failed to store tokens.";
                break;

            default:
                _errorMessage = "Error: An unknown error occurred.";
                break;
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
