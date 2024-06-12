// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using RemoteMaster.Server.Data;

namespace RemoteMaster.Server.Components.Admin.Pages.Manage
{
    public partial class ManageRoleClaims
    {
        private List<IdentityRole> _roles = new();
        private List<IdentityRoleClaim<string>> _roleClaims = new();
        private string? SelectedRoleId { get; set; }
        private RoleClaimEditModel SelectedRoleModel { get; set; } = new();
        private bool ShowSuccessMessage { get; set; } = false;

        private bool HasChanges => !string.IsNullOrEmpty(SelectedRoleModel.ClaimType) && !string.IsNullOrEmpty(SelectedRoleModel.ClaimValue);

        protected async override Task OnInitializedAsync()
        {
            using var scope = ScopeFactory.CreateScope();
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            _roles = await roleManager.Roles.ToListAsync();
        }

        private async Task OnRoleChanged(string roleId)
        {
            SelectedRoleId = roleId;

            using var scope = ScopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            _roleClaims = await dbContext.RoleClaims.Where(rc => rc.RoleId == SelectedRoleId).ToListAsync();

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

            var roleClaim = new IdentityRoleClaim<string>
            {
                RoleId = SelectedRoleId,
                ClaimType = SelectedRoleModel.ClaimType,
                ClaimValue = SelectedRoleModel.ClaimValue
            };

            dbContext.RoleClaims.Add(roleClaim);
            await dbContext.SaveChangesAsync();

            ShowSuccessMessage = true;

            StateHasChanged();

            await HideSuccessMessageAfterDelay();
        }

        private async Task DeleteClaim(IdentityRoleClaim<string> claim)
        {
            using var scope = ScopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            dbContext.RoleClaims.Remove(claim);
            await dbContext.SaveChangesAsync();

            _roleClaims.Remove(claim);

            StateHasChanged();
        }

        private async Task HideSuccessMessageAfterDelay()
        {
            await Task.Delay(3000);
            ShowSuccessMessage = false;

            await InvokeAsync(StateHasChanged);
        }

        public class RoleClaimEditModel
        {
            [Required]
            [Display(Name = "Claim Type")]
            public string ClaimType { get; set; } = string.Empty;

            [Required]
            [Display(Name = "Claim Value")]
            public string ClaimValue { get; set; } = string.Empty;
        }
    }
}
