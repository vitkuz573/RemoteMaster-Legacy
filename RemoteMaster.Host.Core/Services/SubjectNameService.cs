// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using RemoteMaster.Host.Core.Abstractions;

namespace RemoteMaster.Host.Core.Services;

public class SubjectNameService() : ISubjectNameService
{
    private readonly string _organization = "RemoteMaster";
    private readonly string _locality = "Kurgan";
    private readonly string _state = "Kurgan Oblast";
    private readonly string _country = "RU";

    public string GetName(string commonName)
    {
        return $"CN={commonName}, O={_organization}, L={_locality}, ST={_state}, C={_country}";
    }
}
