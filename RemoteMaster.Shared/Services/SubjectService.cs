// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Security.Cryptography.X509Certificates;
using RemoteMaster.Shared.Abstractions;

namespace RemoteMaster.Shared.Services;

public class SubjectService : ISubjectService
{
    public X500DistinguishedName GetDistinguishedName(string commonName, string organization, string[] organizationalUnits, string locality, string state, string country)
    {
        if (organizationalUnits == null || organizationalUnits.Length == 0)
        {
            throw new ArgumentException("Organizational units cannot be null or empty", nameof(organizationalUnits));
        }

        var ous = string.Join(", ", organizationalUnits.Select(ou => $"OU={ou}"));
        var dnString = $"CN={commonName}, O={organization}, {ous}, L={locality}, ST={state}, C={country}";

        return new X500DistinguishedName(dnString);
    }
}
