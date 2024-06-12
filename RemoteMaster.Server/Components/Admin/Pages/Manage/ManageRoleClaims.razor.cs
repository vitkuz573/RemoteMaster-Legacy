using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using RemoteMaster.Server.Data;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RemoteMaster.Server.Components.Admin.Pages.Manage
{
    public partial class ManageRoleClaims
    {
        private List<IdentityRole> _roles = new();
        private List<ClaimTypeViewModel> _claimTypes = new();
        private List<IdentityRoleClaim<string>> _roleClaims = new();
        private string? SelectedRoleId { get; set; }
        private RoleClaimEditModel SelectedRoleModel { get; set; } = new();
        private bool ShowSuccessMessage { get; set; } = false;

        private bool HasChanges => HasChangesInClaims();

        // Stores the initial state of the claims
        private List<(string ClaimType, string ClaimValue)> _initialRoleClaims = new();

        protected async override Task OnInitializedAsync()
        {
            using var scope = ScopeFactory.CreateScope();
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            _roles = await roleManager.Roles.ToListAsync();

            // Load all unique claim types and values from the AspNetRoleClaims table
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var allClaims = await dbContext.RoleClaims
                .GroupBy(rc => rc.ClaimType)
                .Select(g => new ClaimTypeViewModel(
                    g.Key,
                    g.Select(rc => new ClaimValueViewModel { Value = rc.ClaimValue }).Distinct().ToList()
                ))
                .ToListAsync();

            _claimTypes = allClaims;
        }

        private async Task OnRoleChanged(string roleId)
        {
            SelectedRoleId = roleId;
            SelectedRoleModel.Role = _roles.FirstOrDefault(r => r.Id == roleId)?.Name;

            using var scope = ScopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            // Load claims for the selected role
            _roleClaims = await dbContext.RoleClaims.Where(rc => rc.RoleId == SelectedRoleId).ToListAsync();

            // Check the claims that are already assigned to the selected role
            foreach (var claimType in _claimTypes)
            {
                foreach (var value in claimType.Values)
                {
                    value.IsSelected = _roleClaims.Any(rc => rc.ClaimType == claimType.Type && rc.ClaimValue == value.Value);
                }
            }

            // Store the initial state of the selected claims
            _initialRoleClaims = _claimTypes
                .SelectMany(ct => ct.Values.Where(v => v.IsSelected).Select(v => (ct.Type, v.Value)))
                .ToList();

            StateHasChanged();
        }

        private async Task OnValidSubmitAsync()
        {
            if (string.IsNullOrEmpty(SelectedRoleId))
            {
                return;
            }

            using var scope = ScopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var roleClaims = await dbContext.RoleClaims.Where(rc => rc.RoleId == SelectedRoleId).ToListAsync();
            dbContext.RoleClaims.RemoveRange(roleClaims);

            foreach (var claimType in _claimTypes)
            {
                foreach (var value in claimType.Values.Where(v => v.IsSelected))
                {
                    dbContext.RoleClaims.Add(new IdentityRoleClaim<string>
                    {
                        RoleId = SelectedRoleId,
                        ClaimType = claimType.Type,
                        ClaimValue = value.Value
                    });
                }
            }

            await dbContext.SaveChangesAsync();

            ShowSuccessMessage = true;

            // Update the initial state to the current state after saving
            _initialRoleClaims = _claimTypes
                .SelectMany(ct => ct.Values.Where(v => v.IsSelected).Select(v => (ct.Type, v.Value)))
                .ToList();

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
                .SelectMany(ct => ct.Values.Where(v => v.IsSelected).Select(v => (ct.Type, v.Value)))
                .ToList();

            return !_initialRoleClaims.SequenceEqual(selectedRoleClaims);
        }

        private async Task HideSuccessMessageAfterDelay()
        {
            await Task.Delay(3000);
            ShowSuccessMessage = false;

            await InvokeAsync(StateHasChanged);
        }

        public class RoleClaimEditModel
        {
            public string? Role { get; set; }
        }

        public class ClaimTypeViewModel
        {
            public ClaimTypeViewModel(string type, List<ClaimValueViewModel> values)
            {
                Type = type;
                Values = values;
            }

            public string Type { get; set; }

            public bool IsExpanded { get; set; }

            public List<ClaimValueViewModel> Values { get; }
        }

        public class ClaimValueViewModel
        {
            public string Value { get; set; } = string.Empty;

            public bool IsSelected { get; set; }
        }
    }
}
