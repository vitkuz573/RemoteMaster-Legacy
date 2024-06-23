// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using Microsoft.Extensions.Options;
using RemoteMaster.Server.Models;

namespace RemoteMaster.Server.Validators;

public class JwtOptionsValidator : IValidateOptions<JwtOptions>
{
    private readonly int[] ValidKeySizes = [2048, 4096];

    public ValidateOptionsResult Validate(string? name, JwtOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (string.IsNullOrWhiteSpace(options.KeysDirectory))
        {
            return ValidateOptionsResult.Fail("KeysDirectory cannot be null or empty.");
        }

        if (!IsValidPath(options.KeysDirectory))
        {
            return ValidateOptionsResult.Fail("KeysDirectory is not a valid path.");
        }

        if (options.KeySize == null || !ValidKeySizes.Contains(options.KeySize.Value))
        {
            return ValidateOptionsResult.Fail($"KeySize must be one of the following values: {string.Join(", ", ValidKeySizes)}.");
        }

        if (string.IsNullOrWhiteSpace(options.KeyPassword))
        {
            return ValidateOptionsResult.Fail("KeyPassword cannot be null or empty.");
        }

        if (!IsValidPassword(options.KeyPassword))
        {
            return ValidateOptionsResult.Fail("KeyPassword does not meet the complexity requirements.");
        }

        return ValidateOptionsResult.Success;
    }

    private static bool IsValidPath(string path)
    {
        try
        {
            var fullPath = Path.GetFullPath(path);

            return Path.IsPathRooted(fullPath);
        }
        catch
        {
            return false;
        }
    }

    private static bool IsValidPassword(string password)
    {
        if (password.Length < 8)
        {
            return false;
        }
        if (!password.Any(char.IsUpper))
        {
            return false;
        }
        if (!password.Any(char.IsLower))
        {
            return false;
        }
        if (!password.Any(char.IsDigit))
        {
            return false;
        }
        if (!password.Any(ch => "!@#$%^&*()_+-=[]{}|;:,.<>?".Contains(ch)))
        {
            return false;
        }

        return true;
    }
}
