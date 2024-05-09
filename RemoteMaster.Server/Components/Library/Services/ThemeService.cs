// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using RemoteMaster.Server.Components.Library.Abstractions;
using RemoteMaster.Server.Components.Library.Enums;

namespace RemoteMaster.Server.Components.Library.Services;

public class ThemeService : IThemeService
{
    public bool IsDarkTheme { get; set; } = false;

    public Theme Theme => IsDarkTheme ? Theme.Dark : Theme.Light;
}

