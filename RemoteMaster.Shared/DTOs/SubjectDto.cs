﻿// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

namespace RemoteMaster.Shared.DTOs;

public class SubjectDto(string organization, List<string> organizationalUnit)
{
    public string Organization { get; } = organization;

    public List<string> OrganizationalUnit { get; } = organizationalUnit;
}
