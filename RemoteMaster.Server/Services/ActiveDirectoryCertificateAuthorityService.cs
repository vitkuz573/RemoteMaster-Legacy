// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.DirectoryServices.Protocols;
using System.Net;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using FluentResults;
using Microsoft.Extensions.Options;
using RemoteMaster.Server.Abstractions;
using RemoteMaster.Server.Options;
using Serilog;

namespace RemoteMaster.Server.Services;

public class ActiveDirectoryCertificateAuthorityService(IOptions<ActiveDirectoryOptions> options) : ICertificateAuthorityService
{
    private readonly string _activeDirectoryServer = options.Value.ActiveDirectoryServer;
    private readonly string _templateName = options.Value.TemplateName;
    private readonly NetworkCredential _credentials = new(options.Value.Username, options.Value.Password);

    public Result EnsureCaCertificateExists()
    {
        return Result.Ok();
    }

    public Result<X509Certificate2> GetCaCertificate(X509ContentType contentType)
    {
        try
        {
            using var ldapConnection = new LdapConnection(_activeDirectoryServer);
            ldapConnection.Credential = _credentials;
            ldapConnection.AuthType = AuthType.Basic;
            ldapConnection.SessionOptions.ProtocolVersion = 3;
            var optionsValue = options.Value;
            ldapConnection.Bind();

            var searchRequest = new SearchRequest(optionsValue.SearchBase, $"(CN={_templateName})", SearchScope.Subtree, null);

            var searchResponse = (SearchResponse)ldapConnection.SendRequest(searchRequest);

            if (searchResponse.Entries.Count == 0)
            {
                return Result.Fail<X509Certificate2>("Certificate template not found in Active Directory.");
            }

            using var rsa = RSA.Create(options.Value.KeySize);
            var certRequest = new CertificateRequest(new X500DistinguishedName("CN=CommonName"), rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
            var cert = certRequest.CreateSelfSigned(DateTimeOffset.Now, DateTimeOffset.Now.AddYears(options.Value.ValidityPeriod));

            return Result.Ok(cert);
        }
        catch (LdapException ldapEx)
        {
            Log.Error(ldapEx, "LDAP error occurred while retrieving CA certificate from Active Directory.");

            return Result.Fail<X509Certificate2>("LDAP error occurred.").WithError(ldapEx.Message);
        }
        catch (CryptographicException cryptoEx)
        {
            Log.Error(cryptoEx, "Cryptographic error occurred while creating CA certificate.");

            return Result.Fail<X509Certificate2>("Cryptographic error occurred.").WithError(cryptoEx.Message);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to retrieve CA certificate from Active Directory.");

            return Result.Fail<X509Certificate2>("Failed to retrieve CA certificate from Active Directory.").WithError(ex.Message);
        }
    }
}
