// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using RemoteMaster.Server.Data;
using RemoteMaster.Server.Models;

namespace RemoteMaster.Server.Components.Account.Pages;

public partial class Register
{
    private IEnumerable<IdentityError>? _identityErrors;

    [SupplyParameterFromForm]
    private InputModel Input { get; set; } = new();

    [SupplyParameterFromQuery]
    private string? ReturnUrl { get; set; }

    private string? Message => _identityErrors is null ? null : $"Error: {string.Join(", ", _identityErrors.Select(error => error.Description))}";

    public async Task RegisterUser(EditContext editContext)
    {
        if (await RootAdministratorExists())
        {
            _identityErrors =
            [
                new IdentityError
                {
                    Description = "Registration is closed. Only one RootAdministrator is allowed."
                }
            ];

            return;
        }

        var organization = new Organization
        {
            Name = Input.OrganizationName
        };

        await ApplicationDbContext.Organizations.AddAsync(organization);
        await ApplicationDbContext.SaveChangesAsync();

        var user = CreateUser(organization.OrganizationId);

        await UserStore.SetUserNameAsync(user, Input.Email, CancellationToken.None);
        
        var emailStore = GetEmailStore();
        
        await emailStore.SetEmailAsync(user, Input.Email, CancellationToken.None);
        
        var result = await UserManager.CreateAsync(user, Input.Password);

        if (!result.Succeeded)
        {
            _identityErrors = result.Errors;

            return;
        }

        await UserManager.AddToRoleAsync(user, "RootAdministrator");

        Logger.LogInformation("User created a new account with password.");

        var userId = await UserManager.GetUserIdAsync(user);
        var code = await UserManager.GenerateEmailConfirmationTokenAsync(user);
        
        code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));
        
        var callbackUrl = NavigationManager.GetUriWithQueryParameters(NavigationManager.ToAbsoluteUri("Account/ConfirmEmail").AbsoluteUri, new Dictionary<string, object?>
        {
            ["userId"] = userId,
            ["code"] = code,
            ["returnUrl"] = ReturnUrl
        });

        await EmailSender.SendConfirmationLinkAsync(user, Input.Email, HtmlEncoder.Default.Encode(callbackUrl));

        if (UserManager.Options.SignIn.RequireConfirmedAccount)
        {
            RedirectManager.RedirectTo("Account/RegisterConfirmation", new()
            {
                ["email"] = Input.Email,
                ["returnUrl"] = ReturnUrl
            });
        }

        RedirectManager.RedirectTo(ReturnUrl);
    }

    private ApplicationUser CreateUser(Guid organizationId)
    {
        try
        {
            var user = Activator.CreateInstance<ApplicationUser>();
            user.OrganizationId = organizationId;

            return user;
        }
        catch
        {
            throw new InvalidOperationException($"Can't create an instance of '{nameof(ApplicationUser)}'. " +
                $"Ensure that '{nameof(ApplicationUser)}' is not an abstract class and has a parameterless constructor.");
        }
    }

    private IUserEmailStore<ApplicationUser> GetEmailStore()
    {
        if (!UserManager.SupportsUserEmail)
        {
            throw new NotSupportedException("The default UI requires a user store with email support.");
        }

        return (IUserEmailStore<ApplicationUser>)UserStore;
    }

    private async Task<bool> RootAdministratorExists()
    {
        var roleExist = await RoleManager.RoleExistsAsync("RootAdministrator");
        
        if (!roleExist)
        {
            return false;
        }

        var users = await UserManager.GetUsersInRoleAsync("RootAdministrator");

        return users.Any();
    }

    private sealed class InputModel
    {
        [Required]
        [EmailAddress]
        [Display(Name = "Email")]
        public string Email { get; set; } = "";

        [Required]
        [StringLength(100, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        public string Password { get; set; } = "";

        [DataType(DataType.Password)]
        [Display(Name = "Confirm password")]
        [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
        public string ConfirmPassword { get; set; } = "";

        [Required]
        [Display(Name = "Organization Name")]
        public string OrganizationName { get; set; } = "";
    }
}
