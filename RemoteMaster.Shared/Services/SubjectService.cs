// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Security.Cryptography.X509Certificates;
using System.Text;
using Microsoft.Extensions.Logging;
using RemoteMaster.Shared.Abstractions;

namespace RemoteMaster.Shared.Services;

/// <summary>
/// Service for constructing X500DistinguishedName objects with proper escaping as per RFC 2253.
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

            var dnComponents = new List<string>();

            // Order as per RFC 2253: C, ST, L, O, OU, CN (reverse order)
            if (!string.IsNullOrEmpty(country))
            {
                dnComponents.Add($"C={EscapeDnValue(country)}");
            }

            if (!string.IsNullOrEmpty(state))
            {
                dnComponents.Add($"ST={EscapeDnValue(state)}");
            }

            if (!string.IsNullOrEmpty(locality))
            {
                dnComponents.Add($"L={EscapeDnValue(locality)}");
            }

            if (!string.IsNullOrEmpty(organization))
            {
                dnComponents.Add($"O={EscapeDnValue(organization)}");
            }

            foreach (var ou in organizationalUnits)
            {
                dnComponents.Add($"OU={EscapeDnValue(ou)}");
            }
            if (!string.IsNullOrEmpty(commonName))
            {
                dnComponents.Add($"CN={EscapeDnValue(commonName)}");
            }

            var dnString = string.Join(", ", dnComponents);

            logger.LogInformation("Constructed DN string: {DnString}", dnString);

            return new X500DistinguishedName(dnString);
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

    /// <summary>
    /// Escapes special characters in DN values according to RFC 2253.
    /// </summary>
    /// <param name="value">The value to escape.</param>
    /// <returns>The escaped value.</returns>
    private static string EscapeDnValue(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return value;
        }

        // Convert the string to UTF-8 bytes
        var utf8Bytes = Encoding.UTF8.GetBytes(value);
        var escaped = new StringBuilder();

        for (var i = 0; i < utf8Bytes.Length; i++)
        {
            var b = utf8Bytes[i];

            var needsEscaping = false;

            // Check for special characters that must be escaped
            if (b == 0x2C || // ,
                b == 0x2B || // +
                b == 0x22 || // "
                b == 0x5C || // \
                b == 0x3C || // <
                b == 0x3E || // >
                b == 0x3B || // ;
                b == 0x3D)   // =
            {
                needsEscaping = true;
            }

            // '#' must be escaped if it's the first character
            if (b == 0x23 && i == 0) // #
            {
                needsEscaping = true;
            }

            // Space or tab must be escaped if it's the first or last character
            if ((b == 0x20 || b == 0x09) && (i == 0 || i == utf8Bytes.Length - 1))
            {
                needsEscaping = true;
            }

            // Non-printable or non-ASCII characters
            if (b < 0x20 || b > 0x7E)
            {
                escaped.Append('\\');
                escaped.Append(b.ToString("X2"));
                continue;
            }

            if (needsEscaping)
            {
                escaped.Append('\\');
            }

            escaped.Append((char)b);
        }

        return escaped.ToString();
    }
}
