// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

namespace RemoteMaster.Host.Windows.Abstractions;

public interface IOverlayManagerService
{
    IEnumerable<IScreenOverlay> ActiveOverlays { get; }
    
    bool IsOverlayActive(string name);
    
    void ActivateOverlay(string name);
    
    void DeactivateOverlay(string name);
}
