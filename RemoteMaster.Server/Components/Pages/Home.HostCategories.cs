// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Collections.Concurrent;
using System.Net;
using RemoteMaster.Server.Models;
using RemoteMaster.Shared.DTOs;

namespace RemoteMaster.Server.Components.Pages;

public partial class Home
{
    private List<HostCategory> GetHostCategories()
    {
        var categories = new List<HostCategory>();

        var availableCategory = new HostCategory
        {
            Title = "Available hosts",
            CanSelectAll = () => CanSelectAll(_availableHosts),
            CanDeselectAll = () => CanDeselectAll(_availableHosts),
            SelectAllAction = SelectAllAvailableHosts,
            DeselectAllAction = DeselectAllAvailableHosts
        };

        availableCategory.Hosts.AddRange(_availableHosts.Values.ToList());

        var unavailableCategory = new HostCategory
        {
            Title = "Unavailable hosts",
            CanSelectAll = () => CanSelectAll(_unavailableHosts),
            CanDeselectAll = () => CanDeselectAll(_unavailableHosts),
            SelectAllAction = SelectAllUnavailableHosts,
            DeselectAllAction = DeselectAllUnavailableHosts
        };

        unavailableCategory.Hosts.AddRange(_unavailableHosts.Values.ToList());

        var pendingCategory = new HostCategory
        {
            Title = "Pending hosts",
            CanSelectAll = () => CanSelectAll(_pendingHosts),
            CanDeselectAll = () => CanDeselectAll(_pendingHosts),
            SelectAllAction = SelectAllPendingHosts,
            DeselectAllAction = DeselectAllPendingHosts
        };

        pendingCategory.Hosts.AddRange(_pendingHosts.Values.ToList());

        categories.Add(availableCategory);
        categories.Add(unavailableCategory);
        categories.Add(pendingCategory);

        return categories;
    }

    private static IEnumerable<HostDto> GetSortedHosts(IEnumerable<HostDto> hosts)
    {
        return hosts.OrderBy(host => host.Name);
    }

    private Task SelectAllPendingHosts()
    {
        foreach (var host in _pendingHosts.Values)
        {
            SelectHost(host, true);
        }

        return Task.CompletedTask;
    }

    private Task DeselectAllPendingHosts()
    {
        foreach (var host in _pendingHosts.Values)
        {
            SelectHost(host, false);
        }

        return Task.CompletedTask;
    }

    private Task SelectAllAvailableHosts()
    {
        foreach (var host in _availableHosts.Values)
        {
            SelectHost(host, true);
        }

        return Task.CompletedTask;
    }

    private Task DeselectAllAvailableHosts()
    {
        foreach (var host in _availableHosts.Values)
        {
            SelectHost(host, false);
        }

        return Task.CompletedTask;
    }

    private Task SelectAllUnavailableHosts()
    {
        foreach (var host in _unavailableHosts.Values)
        {
            SelectHost(host, true);
        }

        return Task.CompletedTask;
    }

    private Task DeselectAllUnavailableHosts()
    {
        foreach (var host in _unavailableHosts.Values)
        {
            SelectHost(host, false);
        }

        return Task.CompletedTask;
    }

    private bool CanSelectAll(ConcurrentDictionary<IPAddress, HostDto> hosts)
    {
        return hosts.Any(host => !_selectedHosts.Contains(host.Value));
    }

    private bool CanDeselectAll(ConcurrentDictionary<IPAddress, HostDto> hosts)
    {
        return hosts.Any(host => _selectedHosts.Contains(host.Value));
    }
}
