// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Microsoft.JSInterop;
using RemoteMaster.Server.Data;
using RemoteMaster.Server.Entities;

namespace RemoteMaster.Server.Components.Admin.Pages.Manage;

public partial class SignInJournal
{
    private List<SignInEntry> _signInEntries = [];

    private List<SignInEntry> FilteredEntries => FilteredEntriesLogic();

    private List<SignInEntry> PagedEntries => (_sortAscending
        ? FilteredEntries.OrderBy(GetSortKey)
        : FilteredEntries.OrderByDescending(GetSortKey))
        .Skip((_currentPage - 1) * PageSize)
        .Take(PageSize)
        .ToList();

    private string _sortColumn = "SignInTime";
    private bool _sortAscending = true;
    private int _currentPage = 1;
    private int _pageSize = 5;

    private int PageSize
    {
        get => _pageSize;
        set
        {
            _pageSize = value;
            UpdatePagination();
        }
    }
    private int TotalPages => (int)Math.Ceiling((double)FilteredEntries.Count / PageSize);

    private bool HasPreviousPage => _currentPage > 1;

    private bool HasNextPage => _currentPage < TotalPages;

    private string _filter = string.Empty;

    private string Filter
    {
        get => _filter;
        set
        {
            _filter = value;
            ApplyFilter();
        }
    }

    private bool _showFilterHelp;

    private static readonly string[] Columns = ["User", "SignInTime", "Success", "IpAddress"];

    protected async override Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            await JsRuntime.InvokeVoidAsync("setupAutocomplete", "filterInput", Columns);
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
        if (_sortColumn == columnName)
        {
            _sortAscending = !_sortAscending;
        }
        else
        {
            _sortColumn = columnName;
            _sortAscending = true;
        }

        _signInEntries = _sortAscending
            ? [.. _signInEntries.OrderBy(GetSortKey)]
            : [.. _signInEntries.OrderByDescending(GetSortKey)];
    }

    private object GetSortKey(SignInEntry entry)
    {
        return _sortColumn switch
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
            _currentPage++;
        }
    }

    private void PreviousPage()
    {
        if (HasPreviousPage)
        {
            _currentPage--;
        }
    }

    private void UpdatePagination()
    {
        _currentPage = 1;
    }

    private void ApplyFilter()
    {
        _currentPage = 1;
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
            var parts = filter.Split(new[] { '=', '~', '>', '<', '!', ' ' }, 2, StringSplitOptions.RemoveEmptyEntries);

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
            var caseSensitive = filter.Contains("CASE SENSITIVE");

            if (contains)
            {
                filteredEntries = column switch
                {
                    "user" => caseSensitive
                        ? filteredEntries.Where(e => e.User.UserName.Contains(value))
                        : filteredEntries.Where(e => e.User.UserName.Contains(value, StringComparison.OrdinalIgnoreCase)),
                    "signintime" => filteredEntries.Where(e => e.SignInTime.ToString(CultureInfo.InvariantCulture).Contains(value, StringComparison.OrdinalIgnoreCase)),
                    "success" => filteredEntries.Where(e => (e.IsSuccessful ? "Yes" : "No").Contains(value, StringComparison.OrdinalIgnoreCase)),
                    "ipaddress" => filteredEntries.Where(e => e.IpAddress.Contains(value, StringComparison.OrdinalIgnoreCase)),
                    _ => filteredEntries
                };
            }

            if (equals)
            {
                filteredEntries = column switch
                {
                    "user" => caseSensitive
                        ? filteredEntries.Where(e => e.User.UserName.Equals(value))
                        : filteredEntries.Where(e => e.User.UserName.Equals(value, StringComparison.OrdinalIgnoreCase)),
                    "signintime" => filteredEntries.Where(e => e.SignInTime.ToString(CultureInfo.InvariantCulture).Equals(value, StringComparison.OrdinalIgnoreCase)),
                    "success" => filteredEntries.Where(e => (e.IsSuccessful ? "Yes" : "No").Equals(value, StringComparison.OrdinalIgnoreCase)),
                    "ipaddress" => filteredEntries.Where(e => e.IpAddress.Equals(value, StringComparison.OrdinalIgnoreCase)),
                    _ => filteredEntries
                };
            }

            if (notEquals)
            {
                filteredEntries = column switch
                {
                    "user" => caseSensitive
                        ? filteredEntries.Where(e => !e.User.UserName.Equals(value))
                        : filteredEntries.Where(e => !e.User.UserName.Equals(value, StringComparison.OrdinalIgnoreCase)),
                    "signintime" => filteredEntries.Where(e => !e.SignInTime.ToString(CultureInfo.InvariantCulture).Equals(value, StringComparison.OrdinalIgnoreCase)),
                    "success" => filteredEntries.Where(e => !(e.IsSuccessful ? "Yes" : "No").Equals(value, StringComparison.OrdinalIgnoreCase)),
                    "ipaddress" => filteredEntries.Where(e => !e.IpAddress.Equals(value, StringComparison.OrdinalIgnoreCase)),
                    _ => filteredEntries
                };
            }

            if (greaterThan)
            {
                filteredEntries = column switch
                {
                    "signintime" => filteredEntries.Where(e => DateTime.Parse(e.SignInTime.ToString(CultureInfo.InvariantCulture)) > DateTime.Parse(value)),
                    _ => filteredEntries
                };
            }

            if (lessThan)
            {
                filteredEntries = column switch
                {
                    "signintime" => filteredEntries.Where(e => DateTime.Parse(e.SignInTime.ToString(CultureInfo.InvariantCulture)) < DateTime.Parse(value)),
                    _ => filteredEntries
                };
            }
        }

        return [.. filteredEntries];
    }

    private void ShowFilterHelp()
    {
        _showFilterHelp = true;
    }

    private void CloseFilterHelp()
    {
        _showFilterHelp = false;
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
        return _sortColumn != columnName ? string.Empty : _sortAscending ? "↑" : "↓";
    }
}
