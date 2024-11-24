// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using RemoteMaster.Host.Core.Abstractions;

namespace RemoteMaster.Host.Core.NamedOptionsConfigurations;

public class ConfigureJwtBearerOptions(IRsaKeyProvider rsaKeyProvider) : IConfigureNamedOptions<JwtBearerOptions>
{
    public void Configure(string? name, JwtBearerOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var rsa = rsaKeyProvider.GetRsaPublicKey() ?? throw new InvalidOperationException("RSA public key is not available.");
        var validateLifetime = !IsWinPE();

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateIssuerSigningKey = true,
            ValidateLifetime = validateLifetime,
            ValidIssuer = "RemoteMaster Server",
            ValidAudience = "RMServiceAPI",
            IssuerSigningKey = new RsaSecurityKey(rsa),
            RoleClaimType = ClaimTypes.Role,
            AuthenticationType = "JWT Security"
        };
    }

    public void Configure(JwtBearerOptions options)
    {
        Configure(Options.DefaultName, options);
    }

    private static bool IsWinPE()
    {
        var systemDirectory = Environment.SystemDirectory;
        var systemDrive = Path.GetPathRoot(systemDirectory);

        return !string.Equals(systemDrive, @"C:\", StringComparison.OrdinalIgnoreCase);
    }
}