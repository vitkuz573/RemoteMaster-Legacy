// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using RemoteMaster.Server.Components.Library.Enums;

namespace RemoteMaster.Server.Components.Library.Abstractions;

public interface IThemeService
{
    bool IsDarkTheme { get; set; }

    Theme Theme { get; }
}
