// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using RemoteMaster.Host.Core.Abstractions;

namespace RemoteMaster.Host.Core.Services;

public class OverlayManagerService(IEnumerable<IScreenOverlay> screenOverlays) : IOverlayManagerService
{
    private readonly List<IScreenOverlay> _activeOverlays = [];

    public IEnumerable<IScreenOverlay> ActiveOverlays => _activeOverlays;

    public bool IsOverlayActive(string name) => _activeOverlays.Any(o => o.Name.Equals(name, StringComparison.OrdinalIgnoreCase));

    public void ActivateOverlay(string name)
    {
        var overlay = screenOverlays.FirstOrDefault(o => o.Name.Equals(name, StringComparison.OrdinalIgnoreCase));

        if (overlay != null && !_activeOverlays.Contains(overlay))
        {
            _activeOverlays.Add(overlay);
        }
    }

    public void DeactivateOverlay(string name)
    {
        var overlay = _activeOverlays.FirstOrDefault(o => o.Name.Equals(name, StringComparison.OrdinalIgnoreCase));

        if (overlay != null)
        {
            _activeOverlays.Remove(overlay);
        }
    }
}
