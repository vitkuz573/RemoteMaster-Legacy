// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Collections.Concurrent;
using RemoteMaster.Host.Core.Abstractions;

namespace RemoteMaster.Host.Core.Services;

public class OverlayManagerService(IEnumerable<IScreenOverlay> screenOverlays) : IOverlayManagerService
{
    private readonly ConcurrentDictionary<string, HashSet<IScreenOverlay>> _activeOverlaysPerConnection = new();

    public IEnumerable<IScreenOverlay> GetActiveOverlays(string connectionId)
    {
        return _activeOverlaysPerConnection.TryGetValue(connectionId, out var overlays) ? overlays.ToList() : [];
    }

    public bool IsOverlayActive(string name, string connectionId)
    {
        return _activeOverlaysPerConnection.TryGetValue(connectionId, out var overlays) && overlays.Any(o => o.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
    }

    public void ActivateOverlay(string name, string connectionId)
    {
        var overlay = screenOverlays.FirstOrDefault(o => o.Name.Equals(name, StringComparison.OrdinalIgnoreCase)) ?? throw new ArgumentException($"Overlay with name '{name}' not found.");
        var overlays = _activeOverlaysPerConnection.GetOrAdd(connectionId, _ => []);

        overlays.Add(overlay);
    }

    public void DeactivateOverlay(string name, string connectionId)
    {
        if (!_activeOverlaysPerConnection.TryGetValue(connectionId, out var overlays))
        {
            return;
        }

        var overlay = overlays.FirstOrDefault(o => o.Name.Equals(name, StringComparison.OrdinalIgnoreCase));

        if (overlay == null)
        {
            return;
        }

        lock (overlays)
        {
            if (!overlays.Remove(overlay))
            {
                return;
            }

            if (overlays.Count == 0)
            {
                _activeOverlaysPerConnection.TryRemove(connectionId, out _);
            }
        }
    }
}
