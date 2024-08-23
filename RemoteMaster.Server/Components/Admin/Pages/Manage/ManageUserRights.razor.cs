// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using RemoteMaster.Server.Entities;
using RemoteMaster.Server.Enums;

namespace RemoteMaster.Server.Components.Admin.Pages.Manage;

public partial class ManageUserRights
{
    private List<UserViewModel> _users = [];
    private List<OrganizationViewModel> _organizations = [];
    private List<Guid> _initialSelectedOrganizationIds = [];
    private List<Guid> _initialSelectedUnitIds = [];
    private List<IdentityRole> _roles = [];
    private string? _message;

    private string? SelectedUserId { get; set; }

    private UserEditModel SelectedUserModel { get; set; } = new();

    private bool HasChanges => HasChangesInRole() || HasChangesInOrganizations() || HasChangesInUnits() || HasChangesInLockout() || HasChangesInAccessToUnregisteredHosts();

    private const string RootAdminRoleName = "RootAdministrator";

    protected async override Task OnInitializedAsync()
    {
        await LoadRolesAsync();
        await LoadUsersAsync();
        await LoadOrganizationsAsync();
    }

    private async Task LoadRolesAsync()
    {
        _roles = await RoleManager.Roles
            .Where(role => role.Name != RootAdminRoleName)
            .ToListAsync();
    }

    private async Task LoadUsersAsync()
    {
        var users = await UserManager.Users.ToListAsync();

        _users = [];

        foreach (var user in users)
        {
            if (!await UserManager.IsInRoleAsync(user, RootAdminRoleName))
            {
                _users.Add(new UserViewModel
                {
                    Id = user.Id,
                    UserName = user.UserName!,
                    IsLockedOut = user.LockoutEnd != null && user.LockoutEnd > DateTime.UtcNow
                });
            }
        }
    }

    private async Task LoadOrganizationsAsync()
    {
        var organizations = await OrganizationRepository.GetAllAsync();

        if (organizations != null)
        {
            _organizations = organizations
                .Select(o => new OrganizationViewModel(
                    o.Id,
                    o.Name,
                    o.OrganizationalUnits.Select(ou => new OrganizationalUnitViewModel
                    {
                        Id = ou.Id,
                        Name = ou.Name
                    }).ToList()
                ))
                .ToList();
        }
        else
        {
            _message = "Failed to load organizations.";
        }
    }

    private async Task OnValidSubmitAsync()
    {
        if (string.IsNullOrEmpty(SelectedUserId))
        {
            _message = "Error: No user selected.";

            return;
        }

        var user = await UserManager.FindByIdAsync(SelectedUserId);

        if (user == null)
        {
            _message = "Error: User not found.";

            return;
        }

        await UpdateUserRoleAsync(user);
        await UpdateUserAccessAsync(user);
        await UpdateUserLockoutStatusAsync(user);

        user.CanAccessUnregisteredHosts = SelectedUserModel.CanAccessUnregisteredHosts;

        var result = await UserManager.UpdateAsync(user);

        if (result.Succeeded)
        {
            if (_initialSelectedRole != SelectedUserModel.Role)
            {
                await TokenService.RevokeAllRefreshTokensAsync(user.Id, TokenRevocationReason.RoleChanged);

                _message = "User role updated successfully.";
            }
            else
            {
                _message = "User access updated successfully.";
            }
        }
        else
        {
            _message = string.Join("; ", result.Errors.Select(e => e.Description));
        }

        StateHasChanged();
        UpdateInitialSelections();

        NavigationManager.Refresh();
    }

    private async Task UpdateUserRoleAsync(ApplicationUser user)
    {
        var currentRoles = await UserManager.GetRolesAsync(user);

        await UserManager.RemoveFromRolesAsync(user, currentRoles);

        if (!string.IsNullOrEmpty(SelectedUserModel.Role))
        {
            await UserManager.AddToRoleAsync(user, SelectedUserModel.Role);
        }
    }

    private async Task UpdateUserAccessAsync(ApplicationUser user)
    {
        var selectedOrganizationIds = _organizations.Where(o => o.IsSelected).Select(o => o.Id).ToList();
        var selectedUnitIds = _organizations.SelectMany(o => o.OrganizationalUnits).Where(ou => ou.IsSelected).Select(ou => ou.Id).ToList();

        user.UserOrganizations.Clear();
        user.UserOrganizationalUnits.Clear();

        SelectedUserModel.SelectedOrganizations.Clear();
        SelectedUserModel.SelectedOrganizationalUnits.Clear();
        
        foreach (var orgId in selectedOrganizationIds)
        {
            var organization = await OrganizationRepository.GetByIdAsync(orgId);

            if (organization != null)
            {
                user.UserOrganizations.Add(new UserOrganization
                {
                    OrganizationId = orgId,
                    Organization = organization,
                    UserId = user.Id,
                    ApplicationUser = user
                });

                SelectedUserModel.SelectedOrganizations.Add(orgId);
            }
        }

        foreach (var unitId in selectedUnitIds)
        {
            var unit = await OrganizationalUnitRepository.GetByIdAsync(unitId);

            if (unit != null)
            {
                user.UserOrganizationalUnits.Add(new UserOrganizationalUnit
                {
                    OrganizationalUnitId = unitId,
                    OrganizationalUnit = unit,
                    UserId = user.Id,
                    ApplicationUser = user
                });
            }

            SelectedUserModel.SelectedOrganizationalUnits.Add(unitId);
        }
    }

    private Task UpdateUserLockoutStatusAsync(ApplicationUser user)
    {
        if (SelectedUserModel.IsLockedOut)
        {
            user.LockoutEnd = SelectedUserModel.IsPermanentLockout
                ? DateTimeOffset.MaxValue
                : SelectedUserModel.LockoutEndDateTime.ToUniversalTime();
        }
        else
        {
            user.LockoutEnd = null;
        }

        return Task.CompletedTask;
    }

    private async Task LoadCurrentUserAccess()
    {
        if (string.IsNullOrEmpty(SelectedUserId))
        {
            return;
        }

        var user = await UserManager.Users
            .Include(u => u.UserOrganizations)
            .ThenInclude(uo => uo.Organization)
            .Include(u => u.UserOrganizationalUnits)
            .ThenInclude(uou => uou.OrganizationalUnit)
            .FirstOrDefaultAsync(u => u.Id == SelectedUserId);

        if (user == null)
        {
            _message = "Error: User not found.";

            return;
        }

        var userRoles = await UserManager.GetRolesAsync(user);
        var userRole = userRoles.FirstOrDefault();

        SelectedUserModel = new UserEditModel
        {
            Role = userRole,
            IsLockedOut = user.LockoutEnd != null && user.LockoutEnd > DateTime.UtcNow,
            IsPermanentLockout = user.LockoutEnd == DateTimeOffset.MaxValue,
            LockoutEndDateTime = user.LockoutEnd != null && user.LockoutEnd < DateTimeOffset.MaxValue ? user.LockoutEnd.Value.DateTime : DateTime.Now,
            CanAccessUnregisteredHosts = user.CanAccessUnregisteredHosts
        };

        _initialSelectedRole = userRole;
        _initialIsLockedOut = SelectedUserModel.IsLockedOut;
        _initialIsPermanentLockout = SelectedUserModel.IsPermanentLockout;
        _initialLockoutEndDateTime = SelectedUserModel.LockoutEndDateTime;
        _initialCanAccessUnregisteredHosts = SelectedUserModel.CanAccessUnregisteredHosts;

        _initialSelectedOrganizationIds = user.UserOrganizations.Select(ao => ao.OrganizationId).ToList();
        _initialSelectedUnitIds = user.UserOrganizationalUnits.Select(aou => aou.OrganizationalUnitId).ToList();

        foreach (var organization in _organizations)
        {
            organization.IsSelected = _initialSelectedOrganizationIds.Contains(organization.Id);

            if (organization.IsSelected)
            {
                SelectedUserModel.SelectedOrganizations.Add(organization.Id);
            }

            foreach (var unit in organization.OrganizationalUnits)
            {
                unit.IsSelected = _initialSelectedUnitIds.Contains(unit.Id);

                if (unit.IsSelected)
                {
                    SelectedUserModel.SelectedOrganizationalUnits.Add(unit.Id);
                }
            }
        }

        StateHasChanged();
    }

    private async Task OnUserChanged(string? userId)
    {
        SelectedUserId = userId;

        await LoadCurrentUserAccess();
    }

    private static void SelectAllOrganizationalUnits(OrganizationViewModel organization)
    {
        foreach (var unit in organization.OrganizationalUnits)
        {
            unit.IsSelected = true;
        }
    }

    private static void DeselectAllOrganizationalUnits(OrganizationViewModel organization)
    {
        foreach (var unit in organization.OrganizationalUnits)
        {
            unit.IsSelected = false;
        }
    }

    private bool HasChangesInRole() => _initialSelectedRole != SelectedUserModel.Role;

    private bool HasChangesInOrganizations()
    {
        var currentSelectedOrganizationIds = _organizations.Where(o => o.IsSelected).Select(o => o.Id).ToList();

        return !_initialSelectedOrganizationIds.SequenceEqual(currentSelectedOrganizationIds);
    }

    private bool HasChangesInUnits()
    {
        var currentSelectedUnitIds = _organizations.SelectMany(o => o.OrganizationalUnits).Where(ou => ou.IsSelected).Select(ou => ou.Id).ToList();
        
        return !_initialSelectedUnitIds.SequenceEqual(currentSelectedUnitIds);
    }

    private bool HasChangesInLockout()
    {
        return _initialIsLockedOut != SelectedUserModel.IsLockedOut ||
               _initialIsPermanentLockout != SelectedUserModel.IsPermanentLockout ||
               (SelectedUserModel is { IsLockedOut: true, IsPermanentLockout: false } &&
                _initialLockoutEndDateTime != SelectedUserModel.LockoutEndDateTime);
    }

    private bool HasChangesInAccessToUnregisteredHosts()
    {
        return _initialCanAccessUnregisteredHosts != SelectedUserModel.CanAccessUnregisteredHosts;
    }

    private void OnOrganizationChanged(OrganizationViewModel organization)
    {
        if (!organization.IsSelected)
        {
            DeselectAllOrganizationalUnits(organization);
        }

        StateHasChanged();
    }

    private void ToggleOrganizationExpansion(OrganizationViewModel organization)
    {
        organization.IsExpanded = !organization.IsExpanded;

        if (organization.IsExpanded && organization.OrganizationalUnits.Count == 0)
        {
            LoadOrganizationalUnitsAsync(organization.Id).ConfigureAwait(false);
        }

        StateHasChanged();
    }

    private async Task LoadOrganizationalUnitsAsync(Guid organizationId)
    {
        var organizationalUnits = await OrganizationalUnitRepository.GetAllAsync(ou => ou.OrganizationId == organizationId);

        var organization = _organizations.FirstOrDefault(org => org.Id == organizationId);

        if (organization != null)
        {
            organization.OrganizationalUnits.Clear();

            organization.OrganizationalUnits.AddRange(organizationalUnits.Select(ou => new OrganizationalUnitViewModel
            {
                Id = ou.Id,
                Name = ou.Name,
                IsSelected = _initialSelectedUnitIds.Contains(ou.Id)
            }));

            StateHasChanged();
        }
    }

    private void ToggleLockoutDateTimeInputs(ChangeEventArgs e)
    {
        if (e.Value is true)
        {
            return;
        }

        SelectedUserModel.IsPermanentLockout = false;
        SelectedUserModel.LockoutEndDateTime = DateTime.Now;
    }

    private void UpdateInitialSelections()
    {
        _initialSelectedOrganizationIds = _organizations.Where(o => o.IsSelected).Select(o => o.Id).ToList();
        _initialSelectedUnitIds = _organizations.SelectMany(o => o.OrganizationalUnits).Where(ou => ou.IsSelected).Select(ou => ou.Id).ToList();
        _initialSelectedRole = SelectedUserModel.Role;
        _initialIsLockedOut = SelectedUserModel.IsLockedOut;
        _initialIsPermanentLockout = SelectedUserModel.IsPermanentLockout;
        _initialLockoutEndDateTime = SelectedUserModel.LockoutEndDateTime;
        _initialCanAccessUnregisteredHosts = SelectedUserModel.CanAccessUnregisteredHosts;
    }

    public class UserViewModel
    {
        public string Id { get; set; } = string.Empty;

        public string UserName { get; set; } = string.Empty;

        public bool IsLockedOut { get; set; }
    }

    public class OrganizationViewModel(Guid id, string name, List<OrganizationalUnitViewModel> organizationalUnits)
    {
        public Guid Id { get; set; } = id;

        public string Name { get; set; } = name;

        private bool _isSelected;

        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                _isSelected = value;

                if (_isSelected)
                {
                    return;
                }

                foreach (var unit in OrganizationalUnits)
                {
                    unit.IsSelected = false;
                }
            }
        }

        public bool IsExpanded { get; set; }

        public List<OrganizationalUnitViewModel> OrganizationalUnits { get; } = organizationalUnits;
    }

    public class OrganizationalUnitViewModel
    {
        public Guid Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public bool IsSelected { get; set; }
    }

    public class UserEditModel
    {
        [Required]
        [Display(Name = "Role")]
        public string? Role { get; set; }

        public bool IsLockedOut { get; set; }

        public bool IsPermanentLockout { get; set; }

        [Required]
        public DateTime LockoutEndDateTime { get; set; } = DateTime.Now;

        public bool CanAccessUnregisteredHosts { get; set; }

        public List<Guid> SelectedOrganizations { get; } = [];

        public List<Guid> SelectedOrganizationalUnits { get; } = [];
    }

    private string? _initialSelectedRole;
    private bool _initialIsLockedOut;
    private bool _initialIsPermanentLockout;
    private DateTime _initialLockoutEndDateTime;
    private bool _initialCanAccessUnregisteredHosts;
}
