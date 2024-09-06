// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Security.Cryptography.X509Certificates;
using Microsoft.Extensions.Options;
using RemoteMaster.Shared.Abstractions;
using RemoteMaster.Shared.Models;

namespace RemoteMaster.Shared.Services;

public class SubjectService(IOptions<SubjectOptions> options) : ISubjectService
{
    private readonly SubjectOptions _options = options.Value ?? throw new ArgumentNullException(nameof(options));

    public X500DistinguishedName GetDistinguishedName(string commonName)
    {
        var ous = string.Join(", ", _options.OrganizationalUnit.Select(ou => $"OU={ou}"));
        var dnString = $"CN={commonName}, O={_options.Organization}, {ous}, L={_options.Locality}, ST={_options.State}, C={_options.Country}";

        return new X500DistinguishedName(dnString);
    }

    public X500DistinguishedName GetDistinguishedName(string commonName, string locality, string state, string country)
    {
        var ous = string.Join(", ", _options.OrganizationalUnit.Select(ou => $"OU={ou}"));
        var dnString = $"CN={commonName}, O={_options.Organization}, {ous}, L={locality}, ST={state}, C={country}";

        return new X500DistinguishedName(dnString);
    }
}
