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
    private List<IdentityRoleClaim<string>> _roleClaims = [];
    private List<Claim> _initialRoleClaims = [];
    private string? _message;

    private string? SelectedRoleId { get; set; }

    private RoleClaimEditModel SelectedRoleModel { get; set; } = new();

    private bool HasChanges => HasChangesInClaims();

    protected async override Task OnInitializedAsync()
    {
        _roles = await RoleManager.Roles
            .Where(role => role.Name != "RootAdministrator")
            .ToListAsync();

        var allClaims = await DbContext.ApplicationClaims
            .GroupBy(ac => ac.ClaimType)
            .Select(g => new
            {
                ClaimType = g.Key,
                Values = g.Select(ac => ac.ClaimValue).Distinct().ToList()
            })
            .ToListAsync();

        _claimTypes = allClaims.Select(c => new ClaimTypeViewModel(
            c.ClaimType,
            c.Values.Select(v => new ClaimValueViewModel { Value = v }).ToList()
        )).ToList();
    }

    private async Task OnRoleChanged(string? roleId)
    {
        SelectedRoleId = roleId;
        SelectedRoleModel.Role = _roles.FirstOrDefault(r => r.Id == roleId)?.Name;

        _roleClaims = await DbContext.RoleClaims.Where(rc => rc.RoleId == SelectedRoleId).ToListAsync();

        foreach (var claimType in _claimTypes)
        {
            foreach (var value in claimType.Values)
            {
                value.IsSelected = _roleClaims.Any(rc => rc.ClaimType == claimType.Type && rc.ClaimValue == value.Value);
            }
        }

        _initialRoleClaims = _roleClaims
            .Select(rc => new Claim(rc.ClaimType ?? string.Empty, rc.ClaimValue ?? string.Empty))
            .ToList();

        StateHasChanged();
    }

    private async Task OnValidSubmitAsync()
    {
        if (string.IsNullOrEmpty(SelectedRoleId))
        {
            _message = "Error: No role selected.";

            return;
        }

        var existingRoleClaims = await DbContext.RoleClaims.Where(rc => rc.RoleId == SelectedRoleId).ToListAsync();

        var selectedClaims = _claimTypes
            .SelectMany(ct => ct.Values.Where(v => v.IsSelected).Select(v => new Claim(ct.Type, v.Value)))
            .ToList();

        var claimsToRemove = existingRoleClaims
            .Where(rc => !selectedClaims.Any(c => c.Type == rc.ClaimType && c.Value == rc.ClaimValue))
            .ToList();

        var claimsToAdd = selectedClaims
            .Where(sc => !existingRoleClaims.Any(rc => rc.ClaimType == sc.Type && rc.ClaimValue == sc.Value))
            .Select(sc => new IdentityRoleClaim<string>
            {
                RoleId = SelectedRoleId,
                ClaimType = sc.Type,
                ClaimValue = sc.Value
            })
            .ToList();

        DbContext.RoleClaims.RemoveRange(claimsToRemove);
        DbContext.RoleClaims.AddRange(claimsToAdd);

        await DbContext.SaveChangesAsync();

        _message = "Role claims updated successfully.";

        _initialRoleClaims = selectedClaims;

        StateHasChanged();

        await HideSuccessMessageAfterDelay();
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

        if (_initialRoleClaims.Count != selectedRoleClaims.Count)
        {
            return true;
        }

        foreach (var claim in _initialRoleClaims)
        {
            if (!selectedRoleClaims.Any(c => c.Type == claim.Type && c.Value == claim.Value))
            {
                return true;
            }
        }

        return false;
    }

    private async Task HideSuccessMessageAfterDelay()
    {
        await Task.Delay(3000);
        _message = null;

        await InvokeAsync(StateHasChanged);
    }

    public class RoleClaimEditModel
    {
        public string? Role { get; set; }
    }

    public class ClaimTypeViewModel(string type, List<ClaimValueViewModel> values)
    {
        public string Type { get; set; } = type;

        public bool IsExpanded { get; set; }

        public List<ClaimValueViewModel> Values { get; } = values;
    }

    public class ClaimValueViewModel
    {
        public string Value { get; set; } = string.Empty;

        public bool IsSelected { get; set; }
    }
}
