// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using RemoteMaster.Server.Components.Admin.Dialogs;
using RemoteMaster.Server.Data;

namespace RemoteMaster.Server.Components.Admin.Pages.Manage;

public partial class ManageUsers
{
    [SupplyParameterFromForm(FormName = "CreateUser")]
    private InputModel Input { get; set; } = new();

    private IEnumerable<IdentityError>? _identityErrors;
    private List<ApplicationUser> _users = [];
    private readonly Dictionary<ApplicationUser, List<string>> _userRoles = [];
    private readonly Dictionary<ApplicationUser, bool> _userTwoFactorStatus = [];
    private ApplicationUser? _userToDelete;
    private ConfirmationDialog? _confirmationDialog;
    private string? _message;

    private string? Message => _identityErrors is null ? _message : $"Error: {string.Join(", ", _identityErrors.Select(error => error.Description))}";

    protected async override Task OnInitializedAsync()
    {
        await LoadUsersAsync();
    }

    private async Task OnValidSubmitAsync()
    {
        ApplicationUser? user;

        if (Input.Id != null)
        {
            user = await UserManager.FindByIdAsync(Input.Id);

            if (user == null)
            {
                _message = "Error: User not found.";
                
                return;
            }

            user.UserName = Input.Username;

            var updateResult = await UserManager.UpdateAsync(user);

            if (!updateResult.Succeeded)
            {
                _identityErrors = updateResult.Errors;
                StateHasChanged();
                
                return;
            }

            _message = "User updated successfully.";
        }
        else
        {
            user = CreateUser();

            await UserStore.SetUserNameAsync(user, Input.Username, CancellationToken.None);

            var result = await UserManager.CreateAsync(user, Input.Password);

            if (!result.Succeeded)
            {
                _identityErrors = result.Errors;
                StateHasChanged();
                
                return;
            }

            _message = "User created successfully.";
        }

        await LoadUsersAsync();
        
        Input = new InputModel();
        
        NavigationManager.Refresh();
    }

    private void ShowDeleteConfirmation(ApplicationUser user)
    {
        _userToDelete = user;

        var parameters = new Dictionary<string, string>
        {
            { "User", user.UserName }
        };

        _confirmationDialog?.Show(parameters);
    }

    private async Task OnConfirmDelete(bool confirmed)
    {
        if (confirmed && _userToDelete != null)
        {
            await DeleteUserAsync(_userToDelete);
            _userToDelete = null;
        }
    }

    private async Task DeleteUserAsync(ApplicationUser user)
    {
        var result = await UserManager.DeleteAsync(user);

        if (result.Succeeded)
        {
            _users.Remove(user);
            
            await LoadUsersAsync();
            
            _message = "User deleted successfully.";
        }
        else
        {
            _identityErrors = result.Errors;
        }
    }

    private async Task LoadUsersAsync()
    {
        var users = await UserManager.Users.ToListAsync();

        var sortedUsers = new List<ApplicationUser>();

        _userRoles.Clear();
        _userTwoFactorStatus.Clear();

        foreach (var user in users)
        {
            var roles = await UserManager.GetRolesAsync(user);
            var isTwoFactorEnabled = await UserManager.GetTwoFactorEnabledAsync(user);

            _userRoles[user] = [.. roles];
            _userTwoFactorStatus[user] = isTwoFactorEnabled;

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
        public string Password { get; set; } = string.Empty;

        [DataType(DataType.Password)]
        [Display(Name = "Confirm password")]
        [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }

    private sealed class PlaceholderInputModel
    {
        [Required]
        [DataType(DataType.Text)]
        [Display(Name = "Username")]
        public string Username { get; set; }
    }
}
