// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using Microsoft.EntityFrameworkCore;
using RemoteMaster.Server.Data;
using RemoteMaster.Server.Models;

namespace RemoteMaster.Server.Components.Admin.Pages.Manage;

public partial class SignInJournal
{
    private List<SignInEntry> _signInJournalEntries = [];

    private List<SignInEntry> PagedEntries => _signInJournalEntries
                                                .OrderBy(e => e.GetType().GetProperty(SortColumn).GetValue(e, null))
                                                .Skip((CurrentPage - 1) * PageSize)
                                                .Take(PageSize)
                                                .ToList();

    private string SortColumn = "SignInTime";
    private bool SortAscending = true;

    private int CurrentPage = 1;
    private int PageSize = 5;
    private int TotalPages => (int)Math.Ceiling((double)_signInJournalEntries.Count / PageSize);

    private bool HasPreviousPage => CurrentPage > 1;
    private bool HasNextPage => CurrentPage < TotalPages;

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
            ? _signInJournalEntries.OrderBy(e => e.GetType().GetProperty(columnName).GetValue(e, null)).ToList()
            : _signInJournalEntries.OrderByDescending(e => e.GetType().GetProperty(columnName).GetValue(e, null)).ToList();
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

    private async Task ClearJournal()
    {
        using var scope = ScopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        dbContext.SignInEntries.RemoveRange(_signInJournalEntries);
        await dbContext.SaveChangesAsync();

        _signInJournalEntries.Clear();
    }
}
