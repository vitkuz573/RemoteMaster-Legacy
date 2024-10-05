// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using Microsoft.Extensions.Options;
using RemoteMaster.Server.Enums;
using RemoteMaster.Server.Options;

namespace RemoteMaster.Server.Validators;

public class CertificateAuthorityOptionsValidator : IValidateOptions<CertificateAuthorityOptions>
{
    public ValidateOptionsResult Validate(string? name, CertificateAuthorityOptions options)
    {
        if (options == null)
        {
            return ValidateOptionsResult.Fail("CertificateAuthorityOptions cannot be null.");
        }

        switch (options.Type)
        {
            case CaType.Internal:
                if (options.InternalOptions == null)
                {
                    return ValidateOptionsResult.Fail("InternalOptions must be provided when CaType is set to Internal.");
                }
                break;

            case CaType.ActiveDirectory:
                if (options.ActiveDirectoryOptions == null)
                {
                    return ValidateOptionsResult.Fail("ActiveDirectoryOptions must be provided when CaType is set to ActiveDirectory.");
                }
                break;

            default:
                return ValidateOptionsResult.Fail("Unknown CA type.");
        }

        return ValidateOptionsResult.Success;
    }
}
