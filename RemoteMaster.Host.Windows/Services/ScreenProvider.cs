// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using RemoteMaster.Host.Core.Abstractions;
using RemoteMaster.Host.Windows.Helpers.ScreenHelper;

namespace RemoteMaster.Host.Windows.Services;

public class ScreenProvider : IScreenProvider
{
    public IEnumerable<IScreen> GetAllScreens()
    {
        return Screen.AllScreens;
    }

    public IScreen? GetPrimaryScreen()
    {
        return Screen.PrimaryScreen;
    }

    public IScreen GetVirtualScreen()
    {
        return Screen.VirtualScreen;
    }
}
