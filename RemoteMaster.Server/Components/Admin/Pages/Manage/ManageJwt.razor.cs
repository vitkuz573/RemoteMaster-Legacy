// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using Microsoft.AspNetCore.Components;
using RemoteMaster.Server.Models;

namespace RemoteMaster.Server.Components.Admin.Pages.Manage;

public partial class ManageJwt
{
    private string _keysDirectory;
    private int _keySize;

    protected override void OnInitialized()
    {
        var jwt = Jwt.Value;

        _keysDirectory = jwt.KeysDirectory;
        _keySize = jwt.KeySize;
    }
}
