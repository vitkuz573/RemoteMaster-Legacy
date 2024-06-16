// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Text;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Components;
using RemoteMaster.Server.Data;
using Serilog;

namespace RemoteMaster.Server.Components.Account.Pages.Manage;

public partial class EnableAuthenticator
{
    private const string AuthenticatorUriFormat = "otpauth://totp/{0}:{1}?secret={2}&issuer={0}&digits=6";

    private string? _message;
    private ApplicationUser _user = default!;
    private string? _sharedKey;
    private string? _authenticatorUri;
    private IEnumerable<string>? _recoveryCodes;

    [CascadingParameter]
    private HttpContext HttpContext { get; set; } = default!;

    [SupplyParameterFromForm]
    private InputModel Input { get; set; } = new();

    protected async override Task OnInitializedAsync()
    {
        _user = await UserAccessor.GetRequiredUserAsync(HttpContext);

        await LoadSharedKeyAndQrCodeUriAsync(_user);
    }

    private async Task OnValidSubmitAsync()
    {
        var verificationCode = Input.Code.Replace(" ", string.Empty).Replace("-", string.Empty);

        var is2faTokenValid = await UserManager.VerifyTwoFactorTokenAsync(_user, UserManager.Options.Tokens.AuthenticatorTokenProvider, verificationCode);

        if (!is2faTokenValid)
        {
            _message = "Error: Verification code is invalid.";

            return;
        }

        await UserManager.SetTwoFactorEnabledAsync(_user, true);
        var userId = await UserManager.GetUserIdAsync(_user);
        
        Log.Information("User with ID '{UserId}' has enabled 2FA with an authenticator app.", userId);

        _message = "Your authenticator app has been verified.";

        if (await UserManager.CountRecoveryCodesAsync(_user) == 0)
        {
            _recoveryCodes = await UserManager.GenerateNewTwoFactorRecoveryCodesAsync(_user, 10);
        }
        else
        {
            RedirectManager.RedirectToWithStatus("Account/Manage/TwoFactorAuthentication", _message, HttpContext);
        }
    }

    private async ValueTask LoadSharedKeyAndQrCodeUriAsync(ApplicationUser user)
    {
        var unformattedKey = await UserManager.GetAuthenticatorKeyAsync(user);

        if (string.IsNullOrEmpty(unformattedKey))
        {
            await UserManager.ResetAuthenticatorKeyAsync(user);
            unformattedKey = await UserManager.GetAuthenticatorKeyAsync(user);
        }

        _sharedKey = FormatKey(unformattedKey!);

        var username = await UserManager.GetUserNameAsync(user);
        _authenticatorUri = GenerateQrCodeUri(username!, unformattedKey!);
    }

    private static string FormatKey(string unformattedKey)
    {
        var result = new StringBuilder();
        var currentPosition = 0;

        while (currentPosition + 4 < unformattedKey.Length)
        {
            result.Append(unformattedKey.AsSpan(currentPosition, 4)).Append(' ');
            currentPosition += 4;
        }

        if (currentPosition < unformattedKey.Length)
        {
            result.Append(unformattedKey.AsSpan(currentPosition));
        }

        return result.ToString().ToLowerInvariant();
    }

    private string GenerateQrCodeUri(string username, string unformattedKey)
    {
        return string.Format(CultureInfo.InvariantCulture, AuthenticatorUriFormat, UrlEncoder.Encode("RemoteMaster Server"), UrlEncoder.Encode(username), unformattedKey);
    }

    private sealed class InputModel
    {
        [Required]
        [StringLength(7, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 6)]
        [DataType(DataType.Text)]
        [Display(Name = "Verification Code")]
        public string Code { get; set; } = "";
    }
}
