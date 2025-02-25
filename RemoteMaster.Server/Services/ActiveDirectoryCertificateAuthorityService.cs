// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.DirectoryServices;
using System.Security.Cryptography.X509Certificates;
using FluentResults;
using Microsoft.Extensions.Options;
using RemoteMaster.Server.Abstractions;
using RemoteMaster.Server.Options;

namespace RemoteMaster.Server.Services;

public class ActiveDirectoryCertificateAuthorityService(IOptions<ActiveDirectoryOptions> options, ILogger<ActiveDirectoryCertificateAuthorityService> logger) : ICertificateAuthorityService
{
    private readonly ActiveDirectoryOptions _options = options.Value;

    public async Task<Result> EnsureCaCertificateExistsAsync()
    {
        var caCertificateResult = await GetCaCertificateAsync(X509ContentType.Cert);

        if (caCertificateResult.IsFailed)
        {
            logger.LogWarning("CA certificate does not exist or could not be retrieved.");

            return caCertificateResult.ToResult();
        }

        logger.LogInformation("CA certificate exists and is valid.");

        return Result.Ok();
    }

    public Task<Result<X509Certificate2>> GetCaCertificateAsync(X509ContentType contentType)
    {
        try
        {
            var ldapPath = $"LDAP://{_options.Server}:{_options.Port}/{_options.SearchBase}";

            using var directoryEntry = new DirectoryEntry(ldapPath, _options.Username, _options.Password, AuthenticationTypes.Secure);
            using var searcher = new DirectorySearcher(directoryEntry);
            
            searcher.Filter = "(objectClass=certificationAuthority)";
            searcher.SearchScope = SearchScope.Subtree;
            searcher.PropertiesToLoad.Add("cACertificate");

            logger.LogInformation("Searching for CA certificate in Active Directory.");

            var result = searcher.FindOne();

            if (result == null)
            {
                logger.LogWarning("CA certificate not found in Active Directory.");

                return Task.FromResult(Result.Fail<X509Certificate2>("Certificate Authority not found in Active Directory."));
            }

            if (!result.Properties.Contains("cACertificate") || result.Properties["cACertificate"].Count == 0)
            {
                logger.LogWarning("Certificate attribute not found or empty for the given CA.");

                return Task.FromResult(Result.Fail<X509Certificate2>("Certificate attribute not found or empty."));
            }

            var rawData = (byte[])result.Properties["cACertificate"][0];
            var certificate = X509CertificateLoader.LoadCertificate(rawData);

            logger.LogInformation("Successfully retrieved CA certificate from Active Directory.");

            return Task.FromResult(Result.Ok(certificate));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while retrieving the CA certificate from Active Directory.");

            return Task.FromResult(Result.Fail<X509Certificate2>(new ExceptionalError(ex)));
        }
    }
}
