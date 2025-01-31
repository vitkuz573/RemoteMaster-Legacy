// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Drawing;
using RemoteMaster.Host.Core.Abstractions;

namespace RemoteMaster.Host.Linux.Services;

public class TrayIconManager : ITrayIconManager
{
    public void Show() => throw new NotImplementedException();

    public void Hide() => throw new NotImplementedException();

    public void SetIcon(Icon icon) => throw new NotImplementedException();

    public void SetTooltip(string tooltip) => throw new NotImplementedException();

    public void Dispose() => throw new NotImplementedException();
}
