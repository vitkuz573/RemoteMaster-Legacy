// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using RemoteMaster.Server.Components.Admin.Dialogs;

namespace RemoteMaster.Server.Components.Admin.Pages.Manage;

public partial class ManageRoles
{
    private List<IdentityRole> _roles = [];
    
    private readonly IdentityRole _newRole = new()
    {
        Name = string.Empty
    };
    
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
        using var scope = ScopeFactory.CreateScope();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

        var roles = await roleManager.Roles.ToListAsync();

        _roles = roles
            .OrderByDescending(r => r.Name == "RootAdministrator")
            .ThenBy(r => r.Name)
            .ToList();
    }

    private async Task CreateRole()
    {
        if (string.IsNullOrWhiteSpace(_newRole.Name))
        {
            _message = "Role name cannot be empty.";

            return;
        }

        using var scope = ScopeFactory.CreateScope();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

        var result = await roleManager.CreateAsync(new IdentityRole(_newRole.Name));

        if (result.Succeeded)
        {
            _message = "Role created successfully.";
            _newRole.Name = string.Empty;

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
            { "Role", role.Name }
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
        using var scope = ScopeFactory.CreateScope();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

        var result = await roleManager.DeleteAsync(role);

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
}