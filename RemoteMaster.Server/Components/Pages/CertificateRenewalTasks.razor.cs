// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using RemoteMaster.Server.Enums;

namespace RemoteMaster.Server.Components.Pages;

[Authorize]
public partial class CertificateRenewalTasks : ComponentBase
{
    private List<CertificateRenewalTaskViewModel> _certificateTasks = [];

    protected async override Task OnInitializedAsync()
    {
        var tasks = (await CertificateTaskUnitOfWork.CertificateRenewalTasks.GetAllAsync()).ToList();

        var hostIds = tasks.Select(t => t.HostId).Distinct().ToList();

        var hosts = await ApplicationUnitOfWork.Organizations.FindHostsAsync(h => hostIds.Contains(h.Id));

        _certificateTasks = tasks.Select(task =>
        {
            var host = hosts.FirstOrDefault(h => h.Id == task.HostId);

            return new CertificateRenewalTaskViewModel
            {
                Id = task.Id,
                HostName = host?.Name ?? "Unknown",
                PlannedDate = task.RenewalSchedule.PlannedDate,
                LastAttemptDate = task.RenewalSchedule.LastAttemptDate,
                Status = task.Status
            };
        }).ToList();
    }

    private async Task DeleteTask(Guid taskId)
    {
        var taskToRemove = _certificateTasks.FirstOrDefault(t => t.Id == taskId);

        if (taskToRemove != null)
        {
            var dbTask = await CertificateTaskUnitOfWork.CertificateRenewalTasks.GetByIdAsync(taskId);
            
            if (dbTask != null)
            {
                CertificateTaskUnitOfWork.CertificateRenewalTasks.Delete(dbTask);
                
                await CertificateTaskUnitOfWork.CommitAsync();

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
