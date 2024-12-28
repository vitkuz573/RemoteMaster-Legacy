// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using Microsoft.Extensions.Options;
using RemoteMaster.Server.Options;

namespace RemoteMaster.Server.Validators;

public class SubjectOptionsValidator : IValidateOptions<SubjectOptions>
{
    public ValidateOptionsResult Validate(string? name, SubjectOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (string.IsNullOrWhiteSpace(options.Organization))
        {
            return ValidateOptionsResult.Fail("Organization is required.");
        }

        if (options.OrganizationalUnit.Count == 0)
        {
            return ValidateOptionsResult.Fail("At least one OrganizationalUnit is required.");
        }

        if (string.IsNullOrWhiteSpace(options.Locality))
        {
            return ValidateOptionsResult.Fail("Locality is required.");
        }

        if (string.IsNullOrWhiteSpace(options.State))
        {
            return ValidateOptionsResult.Fail("State is required.");
        }

        return string.IsNullOrWhiteSpace(options.Country) ? ValidateOptionsResult.Fail("Country is required.") : ValidateOptionsResult.Success;
    }
}
