// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Net;
using Microsoft.AspNetCore.Identity;
using RemoteMaster.Server.Abstractions;
using RemoteMaster.Server.Aggregates.ApplicationUserAggregate;
using RemoteMaster.Server.Enums;
using RemoteMaster.Server.Models;

namespace RemoteMaster.Server.Services;

public class AuthenticationService(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager, ITokenService tokenService, ITokenStorageService tokenStorageService, IApplicationUserService applicationUserService, IApplicationUnitOfWork applicationUnitOfWork, IEventNotificationService eventNotificationService, ILogger<AuthenticationService> logger) : IAuthenticationService
{
    public async Task<AuthenticationResult> LoginAsync(string username, string password, IPAddress ipAddress, string? returnUrl)
    {
        await applicationUnitOfWork.BeginTransactionAsync();

        try
        {
            var user = await userManager.FindByNameAsync(username);

            if (user == null)
            {
                await eventNotificationService.SendNotificationAsync($"Failed login attempt for username `{username}` from IP `{ipAddress}` at `{DateTime.UtcNow.ToLocalTime()}`.");

                return new AuthenticationResult
                {
                    Status = AuthenticationStatus.InvalidCredentials,
                    ErrorMessage = "Error: Invalid login attempt."
                };
            }

            var userRoles = await userManager.GetRolesAsync(user);

            if (!userRoles.Any())
            {
                await applicationUserService.AddSignInEntryAsync(user, false);

                await eventNotificationService.SendNotificationAsync($"Login attempt for username `{username}` failed due to no roles assigned from IP `{ipAddress}` at `{DateTime.UtcNow.ToLocalTime()}`.");

                return new AuthenticationResult
                {
                    Status = AuthenticationStatus.NoRolesAssigned,
                    ErrorMessage = "Error: User does not belong to any roles."
                };
            }

            var isRootAdmin = await userManager.IsInRoleAsync(user, "RootAdministrator");
            var isLocalhost = IPAddress.IsLoopback(ipAddress);

            if (isRootAdmin && !isLocalhost)
            {
                logger.LogWarning("Attempt to login as RootAdministrator from non-localhost IP.");

                await applicationUserService.AddSignInEntryAsync(user, false);

                await eventNotificationService.SendNotificationAsync($"Unauthorized RootAdministrator login attempt by `{username}` from IP `{ipAddress}` at `{DateTime.UtcNow.ToLocalTime()}`.");

                return new AuthenticationResult
                {
                    Status = AuthenticationStatus.RootAdminAccessDenied,
                    ErrorMessage = "Error: RootAdministrator access is restricted to localhost."
                };
            }

            var result = await signInManager.PasswordSignInAsync(username, password, false, false);

            if (result.Succeeded)
            {
                if (isRootAdmin && isLocalhost)
                {
                    logger.LogInformation("RootAdministrator logged in from localhost. Redirecting to Admin page.");

                    await applicationUserService.AddSignInEntryAsync(user, true);

                    await eventNotificationService.SendNotificationAsync($"RootAdministrator `{username}` successfully logged in from localhost at `{DateTime.UtcNow.ToLocalTime()}`.");

                    await applicationUnitOfWork.CommitTransactionAsync();

                    return new AuthenticationResult
                    {
                        Status = AuthenticationStatus.Success,
                        RedirectUrl = "Admin"
                    };
                }

                await tokenService.RevokeAllRefreshTokensAsync(user.Id, TokenRevocationReason.PreemptiveRevocation);

                logger.LogInformation("User logged in. All previous refresh tokens revoked.");

                await eventNotificationService.SendNotificationAsync($"All previous refresh tokens revoked for user `{username}` at `{DateTime.UtcNow.ToLocalTime()}`.");

                var tokenDataResult = await tokenService.GenerateTokensAsync(user.Id);

                if (!tokenDataResult.IsSuccess)
                {
                    throw new Exception("Error: Failed to generate tokens.");
                }

                var storeTokensResult = await tokenStorageService.StoreTokensAsync(user.Id, tokenDataResult.Value);

                if (!storeTokensResult.IsSuccess)
                {
                    throw new Exception("Error: Failed to store tokens.");
                }

                logger.LogInformation("User {Username} logged in from IP {IPAddress} at {LoginTime}.", username, ipAddress, DateTime.UtcNow.ToLocalTime());

                await eventNotificationService.SendNotificationAsync($"User `{username}` logged in successfully from IP `{ipAddress}` at `{DateTime.UtcNow.ToLocalTime()}`.");

                await applicationUserService.AddSignInEntryAsync(user, true);

                await applicationUnitOfWork.CommitTransactionAsync();

                return new AuthenticationResult
                {
                    Status = AuthenticationStatus.Success,
                    RedirectUrl = returnUrl
                };
            }

            if (result.RequiresTwoFactor)
            {
                await eventNotificationService.SendNotificationAsync($"User `{username}` requires two-factor authentication to log in from IP `{ipAddress}` at `{DateTime.UtcNow.ToLocalTime()}`.");

                await applicationUnitOfWork.CommitTransactionAsync();

                return new AuthenticationResult
                {
                    Status = AuthenticationStatus.RequiresTwoFactor,
                    RedirectUrl = "Account/LoginWith2fa"
                };
            }

            if (!result.IsLockedOut)
            {
                await eventNotificationService.SendNotificationAsync($"Invalid login attempt for username `{username}` from IP `{ipAddress}` at `{DateTime.UtcNow.ToLocalTime()}`.");

                return new AuthenticationResult
                {
                    Status = AuthenticationStatus.InvalidCredentials,
                    ErrorMessage = "Error: Invalid login attempt."
                };
            }

            logger.LogWarning("User with ID '{UserId}' account locked out.", user.Id);

            await eventNotificationService.SendNotificationAsync($"User `{username}` account locked out after login attempt from IP `{ipAddress}` at `{DateTime.UtcNow.ToLocalTime()}`.");

            await applicationUnitOfWork.CommitTransactionAsync();

            return new AuthenticationResult
            {
                Status = AuthenticationStatus.LockedOut,
                ErrorMessage = "Error: Your account has been locked out."
            };

        }
        catch (Exception ex)
        {
            logger.LogError("Error during authentication: {Message}", ex.Message);

            await eventNotificationService.SendNotificationAsync($"Authentication error for username `{username}` from IP `{ipAddress}` at `{DateTime.UtcNow.ToLocalTime()}`. Error: {ex.Message}");

            await applicationUnitOfWork.RollbackTransactionAsync();

            throw new Exception("Error during authentication.", ex);
        }
    }
}
