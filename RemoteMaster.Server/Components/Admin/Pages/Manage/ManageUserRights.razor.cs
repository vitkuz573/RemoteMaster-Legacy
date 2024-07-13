// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using RemoteMaster.Server.Data;
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

    private bool HasChanges => HasChangesInRole() || HasChangesInOrganizations() || HasChangesInUnits() || HasChangesInLockout();

    private const string RootAdminRoleName = "RootAdministrator";

    protected async override Task OnInitializedAsync()
    {
        using var scope = ScopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        await LoadRolesAsync(scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>());
        await LoadUsersAsync(dbContext);
        await LoadOrganizationsAsync(dbContext);
    }

    private async Task LoadRolesAsync(RoleManager<IdentityRole> roleManager)
    {
        _roles = await roleManager.Roles
            .Where(role => role.Name != RootAdminRoleName)
            .ToListAsync();
    }

    private async Task LoadUsersAsync(ApplicationDbContext dbContext)
    {
        var rootAdminRoleId = await dbContext.Roles
            .Where(r => r.Name == RootAdminRoleName)
            .Select(r => r.Id)
            .FirstOrDefaultAsync();

        _users = await dbContext.Users
            .Where(u => !dbContext.UserRoles.Any(ur => ur.UserId == u.Id && ur.RoleId == rootAdminRoleId))
            .Select(u => new UserViewModel
            {
                Id = u.Id,
                UserName = u.UserName!,
                IsLockedOut = u.LockoutEnd != null && u.LockoutEnd > DateTime.UtcNow
            })
            .ToListAsync();
    }

    private async Task LoadOrganizationsAsync(ApplicationDbContext dbContext)
    {
        _organizations = await dbContext.Organizations
            .Include(o => o.OrganizationalUnits)
            .Select(o => new OrganizationViewModel(
                o.NodeId,
                o.Name,
                o.OrganizationalUnits.Select(ou => new OrganizationalUnitViewModel
                {
                    Id = ou.NodeId,
                    Name = ou.Name
                }).ToList()
            )).ToListAsync();
    }

    private async Task OnValidSubmitAsync()
    {
        using var scope = ScopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var user = await dbContext.Users
            .Include(u => u.AccessibleOrganizations)
            .Include(u => u.AccessibleOrganizationalUnits)
            .FirstOrDefaultAsync(u => u.Id == SelectedUserId);

        if (user == null)
        {
            _message = "Error: User not found.";
            
            return;
        }

        await UpdateUserRoleAsync(dbContext, user);
        await UpdateUserAccessAsync(dbContext, user);
        await UpdateUserLockoutStatusAsync(user);

        await dbContext.SaveChangesAsync();

        if (_initialSelectedRole != SelectedUserModel.Role)
        {
            await TokenService.RevokeAllRefreshTokensAsync(user.Id, TokenRevocationReason.RoleChanged);
            _message = "User role updated successfully.";
        }
        else
        {
            _message = "User access updated successfully.";
        }

        StateHasChanged();

        UpdateInitialSelections();

        await HideSuccessMessageAfterDelay();

        NavigationManager.Refresh();
    }

    private async Task UpdateUserRoleAsync(ApplicationDbContext dbContext, ApplicationUser user)
    {
        var userRole = await dbContext.UserRoles.FirstOrDefaultAsync(ur => ur.UserId == user.Id);

        if (userRole != null)
        {
            dbContext.UserRoles.Remove(userRole);
        }

        var role = await dbContext.Roles.FirstOrDefaultAsync(r => r.Name == SelectedUserModel.Role);

        if (role != null)
        {
            dbContext.UserRoles.Add(new IdentityUserRole<string>
            {
                UserId = user.Id,
                RoleId = role.Id
            });
        }
    }

    private async Task UpdateUserAccessAsync(ApplicationDbContext dbContext, ApplicationUser user)
    {
        var selectedOrganizationIds = _organizations.Where(o => o.IsSelected).Select(o => o.Id).ToList();
        var selectedUnitIds = _organizations.SelectMany(o => o.OrganizationalUnits).Where(ou => ou.IsSelected).Select(ou => ou.Id).ToList();

        user.AccessibleOrganizations.Clear();
        user.AccessibleOrganizationalUnits.Clear();

        SelectedUserModel.SelectedOrganizations.Clear();
        SelectedUserModel.SelectedOrganizationalUnits.Clear();

        foreach (var orgId in selectedOrganizationIds)
        {
            var organization = await dbContext.Organizations.FindAsync(orgId);

            if (organization != null)
            {
                user.AccessibleOrganizations.Add(organization);
                SelectedUserModel.SelectedOrganizations.Add(orgId);
            }
        }

        foreach (var unitId in selectedUnitIds)
        {
            var unit = await dbContext.OrganizationalUnits.FindAsync(unitId);

            if (unit != null)
            {
                user.AccessibleOrganizationalUnits.Add(unit);
                SelectedUserModel.SelectedOrganizationalUnits.Add(unitId);
            }
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

    private async Task LoadCurrentUserAccess(ApplicationDbContext dbContext)
    {
        if (string.IsNullOrEmpty(SelectedUserId))
        {
            return;
        }

        var user = await dbContext.Users
            .Include(u => u.AccessibleOrganizations)
            .Include(u => u.AccessibleOrganizationalUnits)
            .FirstOrDefaultAsync(u => u.Id == SelectedUserId);

        if (user == null)
        {
            _message = "Error: User not found.";
            
            return;
        }

        var userRole = await dbContext.UserRoles.Where(ur => ur.UserId == user.Id)
                                                .Join(dbContext.Roles, ur => ur.RoleId, r => r.Id, (ur, r) => r.Name)
                                                .FirstOrDefaultAsync();
        SelectedUserModel = new UserEditModel
        {
            Role = userRole,
            IsLockedOut = user.LockoutEnd != null && user.LockoutEnd > DateTime.UtcNow,
            IsPermanentLockout = user.LockoutEnd == DateTimeOffset.MaxValue,
            LockoutEndDateTime = (user.LockoutEnd != null && user.LockoutEnd < DateTimeOffset.MaxValue) ? user.LockoutEnd.Value.DateTime : DateTime.Now
        };

        _initialSelectedRole = userRole;
        _initialIsLockedOut = SelectedUserModel.IsLockedOut;

        _initialSelectedOrganizationIds = user.AccessibleOrganizations.Select(ao => ao.NodeId).ToList();
        _initialSelectedUnitIds = user.AccessibleOrganizationalUnits.Select(aou => aou.NodeId).ToList();

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

    private async Task OnUserChanged(string userId)
    {
        SelectedUserId = userId;

        using var scope = ScopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await LoadCurrentUserAccess(dbContext);
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
               (SelectedUserModel.IsLockedOut &&
                !SelectedUserModel.IsPermanentLockout &&
                _initialLockoutEndDateTime != SelectedUserModel.LockoutEndDateTime);
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

        StateHasChanged();
    }

    private void ToggleLockoutDateTimeInputs(ChangeEventArgs e)
    {
        if (!(bool)e.Value)
        {
            SelectedUserModel.IsPermanentLockout = false;
            SelectedUserModel.LockoutEndDateTime = DateTime.Now;
        }
    }

    private void UpdateInitialSelections()
    {
        _initialSelectedOrganizationIds = _organizations.Where(o => o.IsSelected).Select(o => o.Id).ToList();
        _initialSelectedUnitIds = _organizations.SelectMany(o => o.OrganizationalUnits).Where(ou => ou.IsSelected).Select(ou => ou.Id).ToList();
        _initialSelectedRole = SelectedUserModel.Role;
        _initialIsLockedOut = SelectedUserModel.IsLockedOut;
        _initialIsPermanentLockout = SelectedUserModel.IsPermanentLockout;
        _initialLockoutEndDateTime = SelectedUserModel.LockoutEndDateTime;
    }

    private async Task HideSuccessMessageAfterDelay()
    {
        await Task.Delay(3000);
        _message = null;

        await InvokeAsync(StateHasChanged);
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
        private bool _isExpanded;

        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                _isSelected = value;

                if (!_isSelected)
                {
                    foreach (var unit in OrganizationalUnits)
                    {
                        unit.IsSelected = false;
                    }
                }
            }
        }

        public bool IsExpanded
        {
            get => _isExpanded;
            set => _isExpanded = value;
        }

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

        public List<Guid> SelectedOrganizations { get; } = [];

        public List<Guid> SelectedOrganizationalUnits { get; } = [];
    }

    private string? _initialSelectedRole;
    private bool _initialIsLockedOut;
    private bool _initialIsPermanentLockout;
    private DateTime _initialLockoutEndDateTime;
}
