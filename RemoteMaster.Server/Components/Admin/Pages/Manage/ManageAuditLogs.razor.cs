// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using Microsoft.AspNetCore.Components;
using RemoteMaster.Server.Aggregates.AuditLogAggregate;

namespace RemoteMaster.Server.Components.Admin.Pages.Manage;

public partial class ManageAuditLogs : ComponentBase
{
    private List<AuditLog> _auditLogs = [];
    
    private bool IsLoading { get; set; } = true;

    // Filters
    private string FilterActionType { get; set; } = string.Empty;
    
    private string FilterUserName { get; set; } = string.Empty;

    // Pagination
    private int PageSize { get; set; } = 10;
    
    private int CurrentPage { get; set; } = 1;
    
    private int TotalPages => (int)Math.Ceiling((double)_auditLogs.Count / PageSize);

    protected async override Task OnInitializedAsync()
    {
        await LoadAuditLogsAsync();
    }

    private async Task LoadAuditLogsAsync()
    {
        try
        {
            IsLoading = true;
            StateHasChanged();

            // Fetch all audit logs (consider implementing server-side paging for large datasets)
            var logs = await AuditLogUnitOfWork.AuditLogs.GetAllAsync();

            _auditLogs = [.. logs.OrderByDescending(al => al.ActionTime)];
            CurrentPage = 1;
        }
        catch (Exception ex)
        {
            Logger.LogError("Error loading audit logs: {Message}", ex.Message);
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task ApplyFilters()
    {
        CurrentPage = 1;

        await FilterAuditLogs();
    }

    private async Task FilterAuditLogs()
    {
        var query = await AuditLogUnitOfWork.AuditLogs.GetAllAsync();

        if (!string.IsNullOrWhiteSpace(FilterActionType))
        {
            query = query.Where(al => al.ActionType.Contains(FilterActionType, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(FilterUserName))
        {
            query = query.Where(al => al.UserName.Contains(FilterUserName, StringComparison.OrdinalIgnoreCase));
        }

        _auditLogs = [.. query.OrderByDescending(al => al.ActionTime)];
    }

    private void NextPage()
    {
        if (HasNextPage)
        {
            CurrentPage++;
        }
    }

    private void PreviousPage()
    {
        if (HasPreviousPage)
        {
            CurrentPage--;
        }
    }

    private bool HasNextPage => CurrentPage < TotalPages;
    private bool HasPreviousPage => CurrentPage > 1;

    private List<AuditLog> PagedAuditLogs => _auditLogs.Skip((CurrentPage - 1) * PageSize).Take(PageSize).ToList();
}
