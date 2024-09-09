// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using Microsoft.Extensions.Options;
using RemoteMaster.Server.Options;
using RemoteMaster.Shared.Models;

namespace RemoteMaster.Server.Validators;

public class CertificateOptionsValidator : IValidateOptions<CertificateOptions>
{
    private readonly int[] _validKeySizes = [2048, 4096];

    public ValidateOptionsResult Validate(string? name, CertificateOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (!_validKeySizes.Contains(options.KeySize))
        {
            return ValidateOptionsResult.Fail($"KeySize must be one of the following values: {string.Join(", ", _validKeySizes)}.");
        }

        if (options.ValidityPeriod <= 0)
        {
            return ValidateOptionsResult.Fail("ValidityPeriod must be a positive integer.");
        }

        if (string.IsNullOrWhiteSpace(options.CommonName))
        {
            return ValidateOptionsResult.Fail("CommonName is required.");
        }

        var subjectValidationResult = ValidateSubjectOptions(options.Subject);

        return !subjectValidationResult.Succeeded ? subjectValidationResult : ValidateOptionsResult.Success;
    }

    private static ValidateOptionsResult ValidateSubjectOptions(SubjectOptions subjectOptions)
    {
        if (string.IsNullOrWhiteSpace(subjectOptions.Organization))
        {
            return ValidateOptionsResult.Fail("Organization is required.");
        }

        if (subjectOptions.OrganizationalUnit.Length == 0)
        {
            return ValidateOptionsResult.Fail("At least one OrganizationalUnit is required.");
        }

        if (string.IsNullOrWhiteSpace(subjectOptions.Locality))
        {
            return ValidateOptionsResult.Fail("Locality is required.");
        }

        if (string.IsNullOrWhiteSpace(subjectOptions.State))
        {
            return ValidateOptionsResult.Fail("State is required.");
        }

        return string.IsNullOrWhiteSpace(subjectOptions.Country) ? ValidateOptionsResult.Fail("Country is required.") : ValidateOptionsResult.Success;
    }
}
