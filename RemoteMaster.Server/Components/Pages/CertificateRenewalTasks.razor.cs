// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using RemoteMaster.Server.Aggregates.OrganizationAggregate;

namespace RemoteMaster.Server.Components.Pages;

[Authorize]
public partial class CertificateRenewalTasks : ComponentBase
{
    private List<CertificateRenewalTask> _certificateTasks = [];

    protected async override Task OnInitializedAsync()
    {
        _certificateTasks = (await OrganizationRepository.GetAllCertificateRenewalTasksAsync()).ToList();
    }

    private async Task DeleteTask(Guid taskId)
    {
        var taskToRemove = _certificateTasks.FirstOrDefault(t => t.Id == taskId);

        if (taskToRemove != null)
        {
            await OrganizationRepository.DeleteCertificateRenewalTaskAsync(taskId);
            await OrganizationRepository.SaveChangesAsync();

            _certificateTasks.Remove(taskToRemove);

            StateHasChanged();
        }
    }
}
