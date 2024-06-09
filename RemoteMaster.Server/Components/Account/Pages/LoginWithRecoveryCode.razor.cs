// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Components;
using RemoteMaster.Server.Data;
using RemoteMaster.Server.Models;
using Serilog;

namespace RemoteMaster.Server.Components.Account.Pages;

public partial class LoginWithRecoveryCode
{
    private string? _message;
    private ApplicationUser _user = default!;

    [CascadingParameter]
    private HttpContext HttpContext { get; set; } = default!;

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
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var recoveryCode = Input.RecoveryCode.Replace(" ", string.Empty);

        var result = await SignInManager.TwoFactorRecoveryCodeSignInAsync(recoveryCode);

        var userId = await UserManager.GetUserIdAsync(_user);

        var userRoles = await UserManager.GetRolesAsync(_user);

        if (!userRoles.Any())
        {
            _message = "Access denied: User does not belong to any roles.";
            await LogSignInAttempt(userId, false, ipAddress);
            return;
        }

        if (result.Succeeded)
        {
            Log.Information("User with ID '{UserId}' logged in with a recovery code.", userId);

            var tokenData = await TokenService.GenerateTokensAsync(userId, ipAddress);

            await TokenStorageService.StoreTokensAsync(userId, tokenData);
            await LogSignInAttempt(userId, true, ipAddress);
            RedirectManager.RedirectTo(ReturnUrl);
        }
        else if (result.IsLockedOut)
        {
            Log.Warning("User account locked out.");
            await LogSignInAttempt(userId, false, ipAddress);
            RedirectManager.RedirectTo("Account/Lockout");
        }
        else
        {
            Log.Warning("Invalid recovery code entered for user with ID '{UserId}' ", userId);
            _message = "Error: Invalid recovery code entered.";
            await LogSignInAttempt(userId, false, ipAddress);
        }
    }

    private async Task LogSignInAttempt(string userId, bool isSuccess, string ipAddress)
    {
        using var scope = ScopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var signInJournalEntry = new SignInEntry
        {
            UserId = userId,
            SignInTime = DateTime.UtcNow,
            IsSuccessful = isSuccess,
            IpAddress = ipAddress
        };

        dbContext.SignInEntries.Add(signInJournalEntry);
        await dbContext.SaveChangesAsync();
    }

    private sealed class InputModel
    {
        [Required]
        [DataType(DataType.Text)]
        [Display(Name = "Recovery Code")]
        public string RecoveryCode { get; set; } = "";
    }
}
