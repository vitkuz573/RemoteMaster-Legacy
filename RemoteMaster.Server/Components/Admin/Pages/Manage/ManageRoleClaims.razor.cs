// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace RemoteMaster.Server.Components.Admin.Pages.Manage;

public partial class ManageRoleClaims
{
    private List<IdentityRole> _roles = [];
    private List<ClaimTypeViewModel> _claimTypes = [];
    private List<Claim> _roleClaims = [];
    private List<Claim> _initialRoleClaims = [];
    private string? _message;

    private string? SelectedRoleId { get; set; }

    private RoleClaimEditModel SelectedRoleModel { get; set; } = new();

    private bool HasChanges => HasChangesInClaims();

    protected async override Task OnInitializedAsync()
    {
        _roles = await RoleManager.Roles
            .Where(role => role.Name != "RootAdministrator" && role.Name != "ServiceUser")
            .ToListAsync();

        var allClaims = (await ApplicationUnitOfWork.ApplicationClaims.GetAllAsync()).ToList();

        if (allClaims.Count != 0)
        {
            var groupedClaims = allClaims
                .GroupBy(ac => ac.ClaimType)
                .Select(g => new
                {
                    ClaimType = g.Key,
                    Values = g.Select(ac => new ClaimValueViewModel
                    {
                        Value = ac.ClaimValue,
                        DisplayName = ac.DisplayName,
                        Description = ac.Description
                    }).Distinct().ToList()
                })
                .ToList();

            _claimTypes = groupedClaims.Select(c => new ClaimTypeViewModel(
                c.ClaimType,
                c.Values
            )).ToList();
        }
        else
        {
            _message = "Error: Failed to load claims.";
        }
    }

    private async Task OnRoleChanged(string? roleId)
    {
        SelectedRoleId = roleId;
        SelectedRoleModel.Role = _roles.FirstOrDefault(r => r.Id == roleId)?.Name;

        if (SelectedRoleId != null)
        {
            var role = await RoleManager.FindByIdAsync(SelectedRoleId);

            if (role != null)
            {
                var roleClaims = await RoleManager.GetClaimsAsync(role);
                _roleClaims = [.. roleClaims];

                foreach (var claimType in _claimTypes)
                {
                    foreach (var value in claimType.Values)
                    {
                        value.IsSelected = _roleClaims.Any(rc => rc.Type == claimType.Type && rc.Value == value.Value);
                    }
                }

                _initialRoleClaims = [.. _roleClaims];
            }

            StateHasChanged();
        }
    }

    private async Task OnValidSubmitAsync()
    {
        if (string.IsNullOrEmpty(SelectedRoleId))
        {
            _message = "Error: No role selected.";
            
            return;
        }

        var role = await RoleManager.FindByIdAsync(SelectedRoleId);

        if (role == null)
        {
            _message = "Error: Role not found.";
            
            return;
        }

        var existingRoleClaims = await RoleManager.GetClaimsAsync(role);

        var selectedClaims = _claimTypes
            .SelectMany(ct => ct.Values.Where(v => v.IsSelected).Select(v => new Claim(ct.Type, v.Value)))
            .ToList();

        var claimsToRemove = existingRoleClaims
            .Where(rc => !selectedClaims.Any(c => c.Type == rc.Type && c.Value == rc.Value))
            .ToList();

        var claimsToAdd = selectedClaims
            .Where(sc => !existingRoleClaims.Any(rc => rc.Type == sc.Type && rc.Value == sc.Value))
            .ToList();

        foreach (var claim in claimsToRemove)
        {
            var result = await RoleManager.RemoveClaimAsync(role, claim);

            if (result.Succeeded)
            {
                continue;
            }

            _message = $"Error: Failed to remove claim {claim.Type}:{claim.Value} from role {role.Name}.";
            
            return;
        }

        foreach (var claim in claimsToAdd)
        {
            var result = await RoleManager.AddClaimAsync(role, claim);

            if (result.Succeeded)
            {
                continue;
            }

            _message = $"Error: Failed to add claim {claim.Type}:{claim.Value} to role {role.Name}.";
            
            return;
        }

        _message = "Role claims updated successfully.";
        
        _initialRoleClaims = selectedClaims;

        StateHasChanged();
    }

    private void ToggleClaimTypeExpansion(ClaimTypeViewModel claimType)
    {
        claimType.IsExpanded = !claimType.IsExpanded;

        StateHasChanged();
    }

    private static void SelectAllClaimValues(ClaimTypeViewModel claimType)
    {
        foreach (var value in claimType.Values)
        {
            value.IsSelected = true;
        }
    }

    private static void DeselectAllClaimValues(ClaimTypeViewModel claimType)
    {
        foreach (var value in claimType.Values)
        {
            value.IsSelected = false;
        }
    }

    private bool HasChangesInClaims()
    {
        var selectedRoleClaims = _claimTypes
            .SelectMany(ct => ct.Values.Where(v => v.IsSelected).Select(v => new Claim(ct.Type, v.Value)))
            .ToList();

        return _initialRoleClaims.Count != selectedRoleClaims.Count || _initialRoleClaims.Any(claim => !selectedRoleClaims.Any(c => c.Type == claim.Type && c.Value == claim.Value));
    }

    public class RoleClaimEditModel
    {
        public string? Role { get; set; }
    }

    public class ClaimTypeViewModel(string type, List<ClaimValueViewModel> values)
    {
        public string Type { get; } = type;

        public bool IsExpanded { get; set; }

        public List<ClaimValueViewModel> Values { get; } = values;
    }

    public class ClaimValueViewModel
    {
        public string Value { get; init; } = string.Empty;

        public string DisplayName { get; init; } = string.Empty;

        public string Description { get; init; } = string.Empty;

        public bool IsSelected { get; set; }
    }
}
