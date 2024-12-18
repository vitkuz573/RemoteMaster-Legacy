// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

namespace RemoteMaster.Host.Core.Abstractions;

public interface IOverlayManagerService
{
    IEnumerable<IScreenOverlay> GetActiveOverlays(string connectionId);

    bool IsOverlayActive(string name, string connectionId);

    void ActivateOverlay(string name, string connectionId);

    void DeactivateOverlay(string name, string connectionId);
}
