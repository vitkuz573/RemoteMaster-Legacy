// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security.Cryptography.X509Certificates;
using Microsoft.Extensions.Logging;
using RemoteMaster.Shared.Abstractions;

namespace RemoteMaster.Shared.Services;

/// <summary>
/// Service for constructing X500DistinguishedName.
/// </summary>
public class SubjectService(ILogger<SubjectService> logger) : ISubjectService
{

    /// <summary>
    /// Creates an <see cref="X500DistinguishedName"/> based on the provided components.
    /// </summary>
    /// <param name="commonName">Common Name (CN).</param>
    /// <param name="organization">Organization (O).</param>
    /// <param name="organizationalUnits">List of Organizational Units (OU).</param>
    /// <param name="locality">Locality (L).</param>
    /// <param name="state">State or Province (ST).</param>
    /// <param name="country">Country (C).</param>
    /// <returns>An <see cref="X500DistinguishedName"/> object.</returns>
    /// <exception cref="ArgumentException">Thrown if <paramref name="organizationalUnits"/> is null or empty.</exception>
    /// <exception cref="ArgumentNullException">Thrown if any required parameter is null or empty.</exception>
    public X500DistinguishedName GetDistinguishedName(string commonName, string organization, IReadOnlyList<string> organizationalUnits, string locality, string state, string country)
    {
        try
        {
            ValidateParameters(commonName, organization, organizationalUnits, locality, state, country);

            var builder = new X500DistinguishedNameBuilder();

            if (!string.IsNullOrEmpty(country))
            {
                builder.AddCountryOrRegion(country);
            }

            if (!string.IsNullOrEmpty(state))
            {
                builder.AddStateOrProvinceName(state);
            }

            if (!string.IsNullOrEmpty(locality))
            {
                builder.AddLocalityName(locality);
            }

            if (!string.IsNullOrEmpty(organization))
            {
                builder.AddOrganizationName(organization);
            }

            foreach (var ou in organizationalUnits)
            {
                if (!string.IsNullOrEmpty(ou))
                {
                    builder.AddOrganizationalUnitName(ou);
                }
                else
                {
                    throw new ArgumentException("Organizational units cannot contain null or empty strings.", nameof(organizationalUnits));
                }
            }

            if (!string.IsNullOrEmpty(commonName))
            {
                builder.AddCommonName(commonName);
            }

            var dn = builder.Build();

            logger.LogInformation("Constructed DN string: {DnString}", dn.Name);

            return dn;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error constructing Distinguished Name.");
            
            throw;
        }
    }

    /// <summary>
    /// Validates the input parameters to ensure they meet the required criteria.
    /// </summary>
    /// <param name="commonName">Common Name.</param>
    /// <param name="organization">Organization.</param>
    /// <param name="organizationalUnits">Organizational Units.</param>
    /// <param name="locality">Locality.</param>
    /// <param name="state">State.</param>
    /// <param name="country">Country.</param>
    /// <exception cref="ArgumentNullException">Thrown if any required parameter is null or empty.</exception>
    /// <exception cref="ArgumentException">Thrown if <paramref name="organizationalUnits"/> is empty.</exception>
    private static void ValidateParameters(string commonName, string organization, IReadOnlyList<string> organizationalUnits, string locality, string state, string country)
    {
        if (string.IsNullOrWhiteSpace(commonName))
        {
            throw new ArgumentNullException(nameof(commonName), "Common name cannot be null or empty.");
        }

        if (string.IsNullOrWhiteSpace(organization))
        {
            throw new ArgumentNullException(nameof(organization), "Organization cannot be null or empty.");
        }

        if (organizationalUnits == null || organizationalUnits.Count == 0)
        {
            throw new ArgumentException("Organizational units cannot be null or empty.", nameof(organizationalUnits));
        }

        if (string.IsNullOrWhiteSpace(locality))
        {
            throw new ArgumentNullException(nameof(locality), "Locality cannot be null or empty.");
        }

        if (string.IsNullOrWhiteSpace(state))
        {
            throw new ArgumentNullException(nameof(state), "State cannot be null or empty.");
        }

        if (string.IsNullOrWhiteSpace(country))
        {
            throw new ArgumentNullException(nameof(country), "Country cannot be null or empty.");
        }

        foreach (var ou in organizationalUnits)
        {
            if (string.IsNullOrWhiteSpace(ou))
            {
                throw new ArgumentException("Organizational units cannot contain null or empty strings.", nameof(organizationalUnits));
            }
        }
    }
}
