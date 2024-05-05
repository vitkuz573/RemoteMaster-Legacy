// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using RemoteMaster.Server.Data;
using RemoteMaster.Server.Models;

namespace RemoteMaster.Server.Components.Admin.Pages;

[Authorize(Roles = "RootAdministrator")]
public partial class ManageUsers
{
    [SupplyParameterFromForm(FormName = "CreateUser")]
    private InputModel Input { get; set; } = new();

    private readonly Dictionary<string, PlaceholderInputModel> _userPlaceholderModels = [];

    private IEnumerable<IdentityError>? _identityErrors;
    private List<ApplicationUser> _users = [];
    private readonly Dictionary<ApplicationUser, List<string>> _userRoles = [];
    private List<IdentityRole> _roles = [];
    private List<Organization> _organizations = [];

    private string? Message => _identityErrors is null ? null : $"Error: {string.Join(", ", _identityErrors.Select(error => error.Description))}";

    protected async override Task OnInitializedAsync()
    {
        await LoadUsers();

        foreach (var user in _users)
        {
            _userPlaceholderModels[user.UserName] = new PlaceholderInputModel { Username = user.UserName };

            var roles = await UserManager.GetRolesAsync(user);

            _userRoles.Add(user, [.. roles]);
        }

        _roles = await RoleManager.Roles
            .Where(role => role.Name != "RootAdministrator")
            .ToListAsync();

        _organizations = [.. ApplicationDbContext.Organizations];
    }

    private async Task OnValidSubmitAsync()
    {
        ApplicationUser user;

        if (Input.Id != null)
        {
            user = await UserManager.FindByIdAsync(Input.Id);

            if (user == null)
            {
                return;
            }

            await UserManager.UpdateAsync(user);
        }
        else
        {
            user = CreateUser();

            await UserStore.SetUserNameAsync(user, Input.Username, CancellationToken.None);

            var result = await UserManager.CreateAsync(user, Input.Password);

            if (!result.Succeeded)
            {
                _identityErrors = result.Errors;

                return;
            }

            await UserManager.AddToRoleAsync(user, Input.Role);

            foreach (var orgId in Input.Organizations)
            {
                var organization = await ApplicationDbContext.Organizations.FindAsync(Guid.Parse(orgId));

                if (organization != null)
                {
                    var userOrganization = new UserOrganization
                    {
                        User = user,
                        Organization = organization
                    };

                    ApplicationDbContext.UserOrganizations.Add(userOrganization);
                }
            }
        }

        await ApplicationDbContext.SaveChangesAsync();

        await LoadUsers();

        NavigationManager.NavigateTo("Admin/Users", true);
    }

    private async Task OnDeleteAsync(ApplicationUser user)
    {
        if (_userPlaceholderModels.TryGetValue(user.UserName, out _))
        {
            await UserManager.DeleteAsync(user);

            _userPlaceholderModels.Remove(user.UserName);
            _users.Remove(user);

            StateHasChanged();
        }
    }

    private async Task EditUser(ApplicationUser user)
    {
        Input = new InputModel
        {
            Id = user.Id,
            Username = user.UserName,
            Role = (await UserManager.GetRolesAsync(user)).FirstOrDefault()
        };

        var userOrganizations = ApplicationDbContext.UserOrganizations
                                   .Where(uo => uo.UserId == user.Id)
                                   .Include(uo => uo.Organization);

        if (userOrganizations != null && await userOrganizations.AnyAsync())
        {
            Input.Organizations = await userOrganizations.Select(uo => uo.OrganizationId.ToString()).ToArrayAsync();
        }
        else
        {
            Input.Organizations = [];
        }
    }

    private async Task LoadUsers()
    {
        _users = await UserManager.Users.ToListAsync();
    }

    private ApplicationUser CreateUser()
    {
        try
        {
            return Activator.CreateInstance<ApplicationUser>();
        }
        catch
        {
            throw new InvalidOperationException($"Can't create an instance of '{nameof(ApplicationUser)}'. " +
                $"Ensure that '{nameof(ApplicationUser)}' is not an abstract class and has a parameterless constructor.");
        }
    }

    private sealed class InputModel
    {
        public string? Id { get; set; }

        [Required]
        [DataType(DataType.Text)]
        [Display(Name = "Username")]
        public string Username { get; set; }

        [Required]
        [Display(Name = "Role")]
        public string Role { get; set; }

        [Required]
        [Display(Name = "Organizations")]
        public string[] Organizations { get; set; } = [];

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

    private sealed class PlaceholderInputModel
    {
        [Required]
        [DataType(DataType.Text)]
        [Display(Name = "Username")]
        public string Username { get; set; }
    }
}
