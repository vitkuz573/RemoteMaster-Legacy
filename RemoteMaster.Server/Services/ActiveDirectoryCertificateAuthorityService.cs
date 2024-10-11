// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.DirectoryServices.Protocols;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using FluentResults;
using Microsoft.Extensions.Options;
using RemoteMaster.Server.Abstractions;
using RemoteMaster.Server.Options;

namespace RemoteMaster.Server.Services;

#pragma warning disable

public class ActiveDirectoryCertificateAuthorityService(IOptions<ActiveDirectoryOptions> options, ILogger<ActiveDirectoryCertificateAuthorityService> logger) : ICertificateAuthorityService
{
    private readonly ActiveDirectoryOptions _options = options.Value;

    public Result EnsureCaCertificateExists()
    {
        var caCertificateResult = GetCaCertificate(X509ContentType.Cert);

        if (caCertificateResult.IsFailed)
        {
            logger.LogWarning("CA certificate does not exist or could not be retrieved.");

            return caCertificateResult.ToResult();
        }

        logger.LogInformation("CA certificate exists and is valid.");

        return Result.Ok();
    }

    public Result<X509Certificate2> GetCaCertificate(X509ContentType contentType)
    {
        try
        {
            var ldapIdentifier = new LdapDirectoryIdentifier(_options.Server, _options.Port);
            var credentials = new NetworkCredential(_options.Username, _options.Password);

            using var connection = new LdapConnection(ldapIdentifier, credentials, AuthType.Basic);

            connection.Bind();

            logger.LogInformation("Successfully connected to LDAP server.");

            var request = new SearchRequest(_options.SearchBase, "(objectClass=certificationAuthority)", SearchScope.Subtree, "cACertificate");

            var response = (SearchResponse)connection.SendRequest(request);

            if (response.Entries.Count == 0)
            {
                logger.LogWarning("No certificate authority found with the specified filter.");

                return Result.Fail("Certificate Authority not found in Active Directory.");
            }

            var entry = response.Entries[0];

            if (!entry.Attributes.Contains("cACertificate"))
            {
                logger.LogWarning("Certificate attribute not found or empty for the given CA.");

                return Result.Fail("Certificate attribute not found or empty.");
            }

            var rawData = (byte[])entry.Attributes["cACertificate"][0];
            var certificate = new X509Certificate2(rawData);

            logger.LogInformation("Successfully retrieved CA certificate.");

            return Result.Ok(certificate);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while retrieving the CA certificate.");

            return Result.Fail(new ExceptionalError(ex));
        }
    }
}
