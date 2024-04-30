// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using RemoteMaster.Server.Data;

namespace RemoteMaster.Server.Components.Admin.Pages;

[Authorize(Roles = "RootAdministrator")]
public partial class ManageUsers
{
    [CascadingParameter]
    private HttpContext HttpContext { get; set; } = default!;

    [SupplyParameterFromForm]
    private InputModel Input { get; set; } = new();

    private IEnumerable<IdentityError>? _identityErrors;
    private ApplicationUser _user = default!;
    private List<ApplicationUser> _users = [];
    private Dictionary<ApplicationUser, List<string>> _userRoles = [];
    private List<IdentityRole> _roles = [];

    private string? Message => _identityErrors is null ? null : $"Error: {string.Join(", ", _identityErrors.Select(error => error.Description))}";

    protected async override Task OnInitializedAsync()
    {
        _user = await UserAccessor.GetRequiredUserAsync(HttpContext);

        _users = await UserManager.Users.ToListAsync();

        _roles = await RoleManager.Roles
            .Where(role => role.Name != "RootAdministrator")
            .ToListAsync();
    }

    private async Task OnValidSubmitAsync()
    {
        var user = CreateUser(_user.OrganizationId);

        await UserStore.SetUserNameAsync(user, Input.Email, CancellationToken.None);

        var emailStore = GetEmailStore();

        await emailStore.SetEmailAsync(user, Input.Email, CancellationToken.None);

        var result = await UserManager.CreateAsync(user, Input.Password);

        if (!result.Succeeded)
        {
            _identityErrors = result.Errors;

            return;
        }

        await UserManager.AddToRoleAsync(user, Input.Role);

        RedirectManager.RedirectToCurrentPageWithStatus("User created", HttpContext);
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

    private sealed class InputModel
    {
        [Required]
        [EmailAddress]
        [Display(Name = "Email")]
        public string Email { get; set; }

        [Required]
        [Display(Name = "Role")]
        public string Role { get; set; }

        [Required]
        [StringLength(100, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        public string Password { get; set; } = "";

        [DataType(DataType.Password)]
        [Display(Name = "Confirm password")]
        [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
        public string ConfirmPassword { get; set; } = "";
    }
}
