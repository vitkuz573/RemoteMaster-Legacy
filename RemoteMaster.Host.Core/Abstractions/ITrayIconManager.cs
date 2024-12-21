// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Drawing;

namespace RemoteMaster.Host.Core.Abstractions;

public interface ITrayIconManager : IDisposable
{
    void Show();

    void Hide();

    void SetIcon(Icon icon);

    void SetTooltip(string tooltip);
}
