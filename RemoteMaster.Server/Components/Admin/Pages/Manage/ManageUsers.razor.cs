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
    [SupplyParameterFromForm(FormName = "CreateUser")]
    private InputModel Input { get; set; } = new();

    private readonly Dictionary<string, PlaceholderInputModel> _userPlaceholderModels = [];

    private IEnumerable<IdentityError>? _identityErrors;
    private List<ApplicationUser> _users = [];
    private readonly Dictionary<ApplicationUser, List<string>> _userRoles = [];
    private List<IdentityRole> _roles = [];

    [Inject]
    private IServiceScopeFactory ScopeFactory { get; set; } = null!;

    private string? Message => _identityErrors is null ? null : $"Error: {string.Join(", ", _identityErrors.Select(error => error.Description))}";

    protected async override Task OnInitializedAsync()
    {
        await LoadRolesAsync();
        await LoadUsersAsync();
    }

    private async Task LoadRolesAsync()
    {
        using var scope = ScopeFactory.CreateScope();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        _roles = await roleManager.Roles
            .Where(role => role.Name != "RootAdministrator")
            .ToListAsync();
    }

    private async Task OnValidSubmitAsync()
    {
        using var scope = ScopeFactory.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var userStore = scope.ServiceProvider.GetRequiredService<IUserStore<ApplicationUser>>();

        ApplicationUser user;

        if (Input.Id != null)
        {
            user = await userManager.FindByIdAsync(Input.Id);

            if (user == null)
            {
                return;
            }

            user.UserName = Input.Username;

            var updateResult = await userManager.UpdateAsync(user);

            if (!updateResult.Succeeded)
            {
                _identityErrors = updateResult.Errors;
                StateHasChanged();

                return;
            }
        }
        else
        {
            user = CreateUser();

            await userStore.SetUserNameAsync(user, Input.Username, CancellationToken.None);

            var result = await userManager.CreateAsync(user, Input.Password);

            if (!result.Succeeded)
            {
                _identityErrors = result.Errors;
                StateHasChanged();

                return;
            }
        }

        await LoadUsersAsync();

        Input = new InputModel();

        NavigationManager.Refresh();
    }

    private async Task OnDeleteAsync(ApplicationUser user)
    {
        using var scope = ScopeFactory.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        if (user != null && _userPlaceholderModels.TryGetValue(user.UserName, out var _))
        {
            await userManager.DeleteAsync(user);

            _userPlaceholderModels.Remove(user.UserName);
            _users.Remove(user);

            StateHasChanged();
        }
    }

    private async Task LoadUsersAsync()
    {
        using var scope = ScopeFactory.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var users = await userManager.Users.ToListAsync();

        var sortedUsers = new List<ApplicationUser>();

        _userPlaceholderModels.Clear();
        _userRoles.Clear();

        foreach (var user in users)
        {
            var roles = await userManager.GetRolesAsync(user);

            _userRoles[user] = new List<string>(roles);
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
    }

    private ApplicationUser CreateUser()
    {
        try
        {
            var user = Activator.CreateInstance<ApplicationUser>();

            return user;
        }
        catch (Exception)
        {
            throw new InvalidOperationException($"Can't create an instance of '{nameof(ApplicationUser)}'. " +
                $"Ensure that '{nameof(ApplicationUser)}' is not an abstract class and has a parameterless constructor.");
        }
    }

    public sealed class InputModel
    {
        public string? Id { get; set; }

        [Required]
        [DataType(DataType.Text)]
        [Display(Name = "Username")]
        public string Username { get; set; }

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
