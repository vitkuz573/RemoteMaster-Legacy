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
using Serilog;

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
    private List<OrganizationalUnit> _organizationalUnits = [];

    private string? Message => _identityErrors is null ? null : $"Error: {string.Join(", ", _identityErrors.Select(error => error.Description))}";

    protected async override Task OnInitializedAsync()
    {
        Log.Information("Initializing ManageUsers component");

        await LoadUsers();

        _roles = await RoleManager.Roles
            .Where(role => role.Name != "RootAdministrator")
            .ToListAsync();

        _organizations = await ApplicationDbContext.Organizations.ToListAsync();
        _organizationalUnits = await ApplicationDbContext.OrganizationalUnits.ToListAsync();
    }

    private async Task OnValidSubmitAsync()
    {
        Log.Information("OnValidSubmitAsync called with Input: {@Input}", Input);

        ApplicationUser user;

        if (Input.Id != null)
        {
            user = await UserManager.FindByIdAsync(Input.Id);

            if (user == null)
            {
                Log.Warning("User with Id {UserId} not found", Input.Id);
                return;
            }

            user.UserName = Input.Username;

            var updateResult = await UserManager.UpdateAsync(user);

            if (!updateResult.Succeeded)
            {
                _identityErrors = updateResult.Errors;
                Log.Error("Failed to update user: {@Errors}", _identityErrors);

                StateHasChanged();

                return;
            }
        }
        else
        {
            user = CreateUser();

            await UserStore.SetUserNameAsync(user, Input.Username, CancellationToken.None);

            var result = await UserManager.CreateAsync(user, Input.Password);

            if (!result.Succeeded)
            {
                _identityErrors = result.Errors;
                Log.Error("Failed to create user: {@Errors}", _identityErrors);
                StateHasChanged();
                return;
            }

            await UserManager.AddToRoleAsync(user, Input.Role);
        }

        if (Input.Role == "Administrator")
        {
            foreach (var orgId in Input.Organizations ?? [])
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
        else if (Input.Role == "Viewer")
        {
            foreach (var ouId in Input.OrganizationalUnits ?? [])
            {
                var organizationalUnit = await ApplicationDbContext.OrganizationalUnits.FindAsync(Guid.Parse(ouId));

                if (organizationalUnit != null)
                {
                    var userOrganizationalUnit = new UserOrganizationalUnit
                    {
                        User = user,
                        OrganizationalUnitId = organizationalUnit.NodeId
                    };

                    ApplicationDbContext.UserOrganizationalUnits.Add(userOrganizationalUnit);
                }
            }
        }

        await ApplicationDbContext.SaveChangesAsync();

        Log.Information("User {Username} successfully created/updated", user.UserName);

        await LoadUsers();

        Input = new InputModel();

        NavigationManager.NavigateTo("Admin/Users", true);
    }

    private async Task OnDeleteAsync(ApplicationUser user)
    {
        if (user != null && _userPlaceholderModels.TryGetValue(user.UserName, out var _))
        {
            await UserManager.DeleteAsync(user);

            _userPlaceholderModels.Remove(user.UserName);
            _users.Remove(user);

            Log.Information("User {Username} successfully deleted", user.UserName);

            StateHasChanged();
        }
    }

    private async Task EditUser(ApplicationUser user)
    {
        if (user != null && _userRoles.TryGetValue(user, out var roles))
        {
            Input = new InputModel
            {
                Id = user.Id,
                Username = user.UserName,
                Role = roles.FirstOrDefault(),
                Organizations = await ApplicationDbContext.UserOrganizations
                                      .Where(uo => uo.UserId == user.Id)
                                      .Select(uo => uo.OrganizationId.ToString())
                                      .ToArrayAsync() ?? []
            };

            Log.Information("Editing user {Username}", user.UserName);
        }
    }

    private async Task LoadUsers()
    {
        Log.Information("Loading users");

        var users = await UserManager.Users.ToListAsync();
        var sortedUsers = new List<ApplicationUser>();

        _userPlaceholderModels.Clear();
        _userRoles.Clear();

        foreach (var user in users)
        {
            var roles = await UserManager.GetRolesAsync(user);

            _userRoles[user] = [.. roles];
            _userPlaceholderModels[user.UserName] = new PlaceholderInputModel
            {
                Username = user.UserName
            };

            if (roles.Contains("RootAdministrator"))
            {
                sortedUsers.Insert(0, user);
            }
            else
            {
                sortedUsers.Add(user);
            }
        }

        _users = sortedUsers;
        Log.Information("Users loaded successfully");
    }

    private ApplicationUser CreateUser()
    {
        try
        {
            var user = Activator.CreateInstance<ApplicationUser>();
            Log.Information("Created a new instance of ApplicationUser");
            return user;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to create an instance of ApplicationUser");
            throw new InvalidOperationException($"Can't create an instance of '{nameof(ApplicationUser)}'. " +
                $"Ensure that '{nameof(ApplicationUser)}' is not an abstract class and has a parameterless constructor.");
        }
    }

    public sealed class InputModel : IValidatableObject
    {
        public string? Id { get; set; }

        [Required]
        [DataType(DataType.Text)]
        [Display(Name = "Username")]
        public string Username { get; set; }

        [Required]
        [Display(Name = "Role")]
        public string Role { get; set; }

        [Display(Name = "Organizations")]
        public string[] Organizations { get; set; } = [];

        [Display(Name = "Organizational Units")]
        public string[] OrganizationalUnits { get; set; } = [];

        [Required]
        [StringLength(100, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        public string Password { get; set; } = "";

        [DataType(DataType.Password)]
        [Display(Name = "Confirm password")]
        [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
        public string ConfirmPassword { get; set; } = "";

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (Role == "Administrator" && (Organizations == null || Organizations.Length == 0))
            {
                yield return new ValidationResult("Organizations are required for Administrators.", [nameof(Organizations)]);
            }
            if (Role == "Viewer" && (OrganizationalUnits == null || OrganizationalUnits.Length == 0))
            {
                yield return new ValidationResult("Organizational Units are required for Viewers.", [nameof(OrganizationalUnits)]);
            }
        }
    }

    private sealed class PlaceholderInputModel
    {
        [Required]
        [DataType(DataType.Text)]
        [Display(Name = "Username")]
        public string Username { get; set; }
    }
}
