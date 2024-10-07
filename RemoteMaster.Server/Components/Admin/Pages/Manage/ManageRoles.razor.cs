// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using RemoteMaster.Server.Components.Admin.Dialogs;

namespace RemoteMaster.Server.Components.Admin.Pages.Manage;

public partial class ManageRoles
{
    [SupplyParameterFromForm]
    private InputModel Input { get; set; } = new();

    private List<IdentityRole> _roles = [];

    private IdentityRole? _roleToDelete;
    private ConfirmationDialog? _confirmationDialog;

    private IEnumerable<IdentityError>? _identityErrors;
    private string? _message;

    private string? Message => _identityErrors is null ? _message : $"Error: {string.Join(", ", _identityErrors.Select(error => error.Description))}";

    protected async override Task OnInitializedAsync()
    {
        await LoadRolesAsync();
    }

    private async Task LoadRolesAsync()
    {
        var roles = await RoleManager.Roles.ToListAsync();

        _roles = [.. roles
            .OrderByDescending(r => r.Name == "RootAdministrator")
            .ThenByDescending(r => r.Name == "ServiceUser")
            .ThenBy(r => r.Name)];
    }

    private async Task CreateRole()
    {
        var result = await RoleManager.CreateAsync(new IdentityRole(Input.Name));

        if (result.Succeeded)
        {
            _message = "Role created successfully.";

            Input = new InputModel();

            await LoadRolesAsync();
        }
        else
        {
            _message = $"Error creating role: {string.Join(", ", result.Errors.Select(e => e.Description))}";
        }
    }

    private void ShowDeleteConfirmation(IdentityRole role)
    {
        _roleToDelete = role;

        var parameters = new Dictionary<string, string>
        {
            { "Role", role.Name! }
        };

        _confirmationDialog?.Show(parameters);
    }

    private async Task OnConfirmDelete(bool confirmed)
    {
        if (confirmed && _roleToDelete != null)
        {
            await DeleteRoleAsync(_roleToDelete);

            _roleToDelete = null;
        }
    }

    private async Task DeleteRoleAsync(IdentityRole role)
    {
        var result = await RoleManager.DeleteAsync(role);

        if (result.Succeeded)
        {
            await LoadRolesAsync();

            _message = "Role deleted successfully.";
        }
        else
        {
            _identityErrors = result.Errors;
        }
    }

    public class InputModel
    {
        [Required]
        [DataType(DataType.Text)]
        [Display(Name = "Name")]
        public string Name { get; set; } = string.Empty;
    }
}
