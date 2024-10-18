// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using RemoteMaster.Server.Enums;

namespace RemoteMaster.Server.Components.Pages;

[Authorize]
public partial class CertificateRenewalTasks : ComponentBase
{
    private List<CertificateRenewalTaskViewModel> _certificateTasks = [];

    protected async override Task OnInitializedAsync()
    {
        var tasks = await CertificateTaskDbContext.CertificateRenewalTasks.ToListAsync();

        var hostIds = tasks.Select(t => t.HostId).Distinct().ToList();

        var hosts = await ApplicationUnitOfWork.Organizations.FindHostsAsync(h => hostIds.Contains(h.Id));

        _certificateTasks = tasks.Select(task =>
        {
            var host = hosts.FirstOrDefault(h => h.Id == task.HostId);

            return new CertificateRenewalTaskViewModel
            {
                Id = task.Id,
                HostName = host?.Name ?? "Unknown",
                PlannedDate = task.PlannedDate,
                LastAttemptDate = task.LastAttemptDate,
                Status = task.Status
            };
        }).ToList();
    }

    private async Task DeleteTask(Guid taskId)
    {
        var taskToRemove = _certificateTasks.FirstOrDefault(t => t.Id == taskId);

        if (taskToRemove != null)
        {
            var dbTask = await CertificateTaskDbContext.CertificateRenewalTasks.FindAsync(taskId);
            
            if (dbTask != null)
            {
                CertificateTaskDbContext.CertificateRenewalTasks.Remove(dbTask);
                
                await CertificateTaskDbContext.SaveChangesAsync();

                _certificateTasks.Remove(taskToRemove);

                StateHasChanged();
            }
        }
    }

    public class CertificateRenewalTaskViewModel
    {
        public Guid Id { get; set; }
        
        public string HostName { get; set; }
        
        public DateTimeOffset PlannedDate { get; set; }
        
        public DateTimeOffset? LastAttemptDate { get; set; }
        
        public CertificateRenewalStatus Status { get; set; }
    }
}
