// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using RemoteMaster.Server.Abstractions;

namespace RemoteMaster.Server.Services;

public class BrandingService : IBrandingService
{
    public string ApplicationName => "RemoteMaster хуй";

    public string ApplicationLogo => "/img/logo.png";

    public string ApplicationLogoWidth => "300";

    public string ApplicationLogoHeight => "auto";
}
