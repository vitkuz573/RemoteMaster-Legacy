// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using Microsoft.EntityFrameworkCore;
using Microsoft.JSInterop;
using RemoteMaster.Server.Data;
using RemoteMaster.Server.Models;

namespace RemoteMaster.Server.Components.Admin.Pages.Manage;

public partial class SignInJournal
{
    private List<SignInEntry> _signInEntries = [];

    private List<SignInEntry> FilteredEntries => FilteredEntriesLogic();

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

    private bool showFilterHelp = false;

    private static readonly string[] _columns = ["User", "SignInTime", "Success", "IpAddress"];

    protected async override Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            await JSRuntime.InvokeVoidAsync("setupAutocomplete", "filterInput", _columns);
        }
    }

    protected async override Task OnInitializedAsync()
    {
        using var scope = ScopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        _signInEntries = await dbContext.SignInEntries
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

        _signInEntries = SortAscending
            ? [.. _signInEntries.OrderBy(GetSortKey)]
            : [.. _signInEntries.OrderByDescending(GetSortKey)];
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

    private List<SignInEntry> FilteredEntriesLogic()
    {
        if (string.IsNullOrWhiteSpace(Filter))
        {
            return _signInEntries;
        }

        var filters = Filter.Split(';');
        var filteredEntries = _signInEntries.AsQueryable();

        foreach (var filter in filters)
        {
            var parts = filter.Split(['=', '~', '>', '<', '!'], 2);

            if (parts.Length < 2)
            {
                continue;
            }

            var column = parts[0].Trim().ToLower();
            var value = parts[1].Trim();
            var contains = filter.Contains('~');
            var equals = filter.Contains('=');
            var notEquals = filter.Contains('!');
            var greaterThan = filter.Contains('>');
            var lessThan = filter.Contains('<');

            if (contains)
            {
                filteredEntries = column switch
                {
                    "user" => filteredEntries.Where(e => e.User.UserName.Contains(value, StringComparison.OrdinalIgnoreCase)),
                    "signintime" => filteredEntries.Where(e => e.SignInTime.ToString().Contains(value, StringComparison.OrdinalIgnoreCase)),
                    "success" => filteredEntries.Where(e => (e.IsSuccessful ? "Yes" : "No").Contains(value, StringComparison.OrdinalIgnoreCase)),
                    "ipaddress" => filteredEntries.Where(e => e.IpAddress.Contains(value, StringComparison.OrdinalIgnoreCase)),
                    _ => filteredEntries
                };
            }

            if (equals)
            {
                filteredEntries = column switch
                {
                    "user" => filteredEntries.Where(e => e.User.UserName.Equals(value, StringComparison.OrdinalIgnoreCase)),
                    "signintime" => filteredEntries.Where(e => e.SignInTime.ToString().Equals(value, StringComparison.OrdinalIgnoreCase)),
                    "success" => filteredEntries.Where(e => (e.IsSuccessful ? "Yes" : "No").Equals(value, StringComparison.OrdinalIgnoreCase)),
                    "ipaddress" => filteredEntries.Where(e => e.IpAddress.Equals(value, StringComparison.OrdinalIgnoreCase)),
                    _ => filteredEntries
                };
            }

            if (notEquals)
            {
                filteredEntries = column switch
                {
                    "user" => filteredEntries.Where(e => !e.User.UserName.Equals(value, StringComparison.OrdinalIgnoreCase)),
                    "signintime" => filteredEntries.Where(e => !e.SignInTime.ToString().Equals(value, StringComparison.OrdinalIgnoreCase)),
                    "success" => filteredEntries.Where(e => !(e.IsSuccessful ? "Yes" : "No").Equals(value, StringComparison.OrdinalIgnoreCase)),
                    "ipaddress" => filteredEntries.Where(e => !e.IpAddress.Equals(value, StringComparison.OrdinalIgnoreCase)),
                    _ => filteredEntries
                };
            }

            if (greaterThan)
            {
                filteredEntries = column switch
                {
                    "signintime" => filteredEntries.Where(e => DateTime.Parse(e.SignInTime.ToString()) > DateTime.Parse(value)),
                    _ => filteredEntries
                };
            }

            if (lessThan)
            {
                filteredEntries = column switch
                {
                    "signintime" => filteredEntries.Where(e => DateTime.Parse(e.SignInTime.ToString()) < DateTime.Parse(value)),
                    _ => filteredEntries
                };
            }
        }

        return [.. filteredEntries];
    }

    private void ShowFilterHelp()
    {
        showFilterHelp = true;
    }

    private void CloseFilterHelp()
    {
        showFilterHelp = false;
    }

    private async Task ClearJournal()
    {
        using var scope = ScopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        dbContext.SignInEntries.RemoveRange(_signInEntries);
        await dbContext.SaveChangesAsync();

        _signInEntries.Clear();
    }

    private string GetSortIcon(string columnName)
    {
        return SortColumn != columnName ? string.Empty : SortAscending ? "↑" : "↓";
    }
}
