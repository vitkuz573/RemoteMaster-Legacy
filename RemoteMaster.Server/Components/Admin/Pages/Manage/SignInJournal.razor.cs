// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using Microsoft.EntityFrameworkCore;
using RemoteMaster.Server.Data;
using RemoteMaster.Server.Models;

namespace RemoteMaster.Server.Components.Admin.Pages.Manage;

public partial class SignInJournal
{
    private List<SignInEntry> _signInJournalEntries = new();
    private List<SignInEntry> FilteredEntries => string.IsNullOrWhiteSpace(Filter)
        ? _signInJournalEntries
        : _signInJournalEntries.Where(e =>
            e.User.UserName.Contains(Filter, StringComparison.OrdinalIgnoreCase) ||
            e.IpAddress.Contains(Filter, StringComparison.OrdinalIgnoreCase) ||
            e.SignInTime.ToString().Contains(Filter, StringComparison.OrdinalIgnoreCase)).ToList();

    private List<SignInEntry> PagedEntries => (SortAscending
        ? FilteredEntries.OrderBy(GetSortKey)
        : FilteredEntries.OrderByDescending(GetSortKey))
        .Skip((CurrentPage - 1) * PageSize)
        .Take(PageSize)
        .ToList();

    private string SortColumn = "SignInTime";
    private bool SortAscending = true;

    private int CurrentPage = 1;
    private int pageSize = 5;
    private int PageSize
    {
        get => pageSize;
        set
        {
            pageSize = value;
            UpdatePagination();
        }
    }
    private int TotalPages => (int)Math.Ceiling((double)FilteredEntries.Count / PageSize);

    private bool HasPreviousPage => CurrentPage > 1;
    private bool HasNextPage => CurrentPage < TotalPages;

    private string filter = string.Empty;
    private string Filter
    {
        get => filter;
        set
        {
            filter = value;
            ApplyFilter();
        }
    }

    protected async override Task OnInitializedAsync()
    {
        using var scope = ScopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        _signInJournalEntries = await dbContext.SignInEntries
            .Include(entry => entry.User)
            .OrderByDescending(e => e.SignInTime)
            .ToListAsync();
    }

    private void SortByColumn(string columnName)
    {
        if (SortColumn == columnName)
        {
            SortAscending = !SortAscending;
        }
        else
        {
            SortColumn = columnName;
            SortAscending = true;
        }

        _signInJournalEntries = SortAscending
            ? _signInJournalEntries.OrderBy(GetSortKey).ToList()
            : _signInJournalEntries.OrderByDescending(GetSortKey).ToList();
    }

    private object GetSortKey(SignInEntry entry)
    {
        return SortColumn switch
        {
            "User" => entry.User.UserName,
            "SignInTime" => entry.SignInTime,
            "Success" => entry.IsSuccessful,
            "IpAddress" => entry.IpAddress,
            _ => entry.SignInTime
        };
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

    private void UpdatePagination()
    {
        CurrentPage = 1;
    }

    private void ApplyFilter()
    {
        CurrentPage = 1;
    }

    private async Task ClearJournal()
    {
        using var scope = ScopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        dbContext.SignInEntries.RemoveRange(_signInJournalEntries);
        await dbContext.SaveChangesAsync();

        _signInJournalEntries.Clear();
    }

    private string GetSortIcon(string columnName)
    {
        return SortColumn != columnName ? string.Empty : SortAscending ? "↑" : "↓";
    }
}
