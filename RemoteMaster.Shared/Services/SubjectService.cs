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

            var dnComponents = new List<string>
            {
                $"CN={EscapeDnValue(commonName)}",
                $"O={EscapeDnValue(organization)}"
            };

            dnComponents.AddRange(organizationalUnits.Select(ou => $"OU={EscapeDnValue(ou)}"));
            dnComponents.Add($"L={EscapeDnValue(locality)}");
            dnComponents.Add($"ST={EscapeDnValue(state)}");
            dnComponents.Add($"C={EscapeDnValue(country)}");

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

        var escaped = new StringBuilder();
        
        for (var i = 0; i < value.Length; i++)
        {
            var c = value[i];
            // Characters that need to be escaped according to RFC 2253
            if (c == ',' || c == '+' || c == '"' || c == '\\' || c == '<' ||
                c == '>' || c == ';' || c == '=' || c == '#')
            {
                escaped.Append('\\');
            }

            // If the first character is a space or '#', it needs to be escaped
            if (i == 0 && (c == ' ' || c == '#'))
            {
                escaped.Append('\\');
            }

            // If the last character is a space, it needs to be escaped
            if (i == value.Length - 1 && c == ' ')
            {
                escaped.Append('\\');
            }

            escaped.Append(c);
        }

        // Convert to UTF-8 encoding to ensure proper representation
        return escaped.ToString();
    }
}
