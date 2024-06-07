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

    protected async override Task OnInitializedAsync()
    {
        using var scope = ScopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        _signInJournalEntries = await dbContext.SignInEntries
            .Include(entry => entry.User)
            .OrderByDescending(e => e.SignInTime)
            .ToListAsync();
    }
}
