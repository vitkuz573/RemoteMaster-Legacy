// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

namespace RemoteMaster.Shared.DTOs;

public class AddressDto(string locality, string state, string country)
{
    public string Locality { get; } = locality;

    public string State { get; } = state;

    public string Country { get; } = country;
}
