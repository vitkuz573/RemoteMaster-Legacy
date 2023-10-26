// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using Microsoft.Extensions.Logging;
using Org.BouncyCastle.Asn1;
using Org.BouncyCastle.Asn1.Pkcs;
using Org.BouncyCastle.Asn1.X509;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.OpenSsl;
using Org.BouncyCastle.Pkcs;
using Org.BouncyCastle.Security;
using RemoteMaster.Host.Core.Abstractions;

namespace RemoteMaster.Host.Core.Services;

public class CertificateRequestService : ICertificateRequestService
{
    private readonly ILogger<CertificateRequestService> _logger;

    public CertificateRequestService(ILogger<CertificateRequestService> logger)
    {
        _logger = logger;
    }

    public Pkcs10CertificationRequest GenerateCSR(string subjectName, List<string> ipAddresses, out AsymmetricCipherKeyPair keyPair)
    {
        _logger.LogInformation("Starting CSR generation for subject: {SubjectName}", subjectName);

        var keyGenerationParameters = new KeyGenerationParameters(new SecureRandom(), 2048);
        var keyPairGenerator = new RsaKeyPairGenerator();
        keyPairGenerator.Init(keyGenerationParameters);
        keyPair = keyPairGenerator.GenerateKeyPair();

        _logger.LogDebug("Key pair generated successfully with length {KeyLength}.", 2048);

        var csrStream = new MemoryStream();
        using var csrWriter = new PemWriter(new StreamWriter(csrStream));

        var subject = new X509Name(subjectName);

        var genNames = ipAddresses.Select(ip => new GeneralName(GeneralName.IPAddress, ip)).ToArray();

        var extensionsGenerator = new X509ExtensionsGenerator();
        extensionsGenerator.AddExtension(X509Extensions.SubjectAlternativeName, false, new DerSequence(genNames));
        var extensions = extensionsGenerator.Generate();

        var attributePkcs = new AttributePkcs(PkcsObjectIdentifiers.Pkcs9AtExtensionRequest, new DerSet(extensions));

        var csr = new Pkcs10CertificationRequest("SHA256WITHRSA", subject, keyPair.Public, new DerSet(attributePkcs), keyPair.Private);

        csrWriter.WriteObject(csr);
        csrWriter.Writer.Flush();

        _logger.LogInformation("CSR generated successfully for subject: {SubjectName}.", subjectName);
        _logger.LogDebug("CSR Signature Algorithm: {SignatureAlgorithm}", csr.SignatureAlgorithm.Algorithm.Id);
        _logger.LogDebug("CSR Version: {Version}", csr.GetCertificationRequestInfo().Version);
        _logger.LogDebug("CSR Subject: {CSRSubject}", csr.GetCertificationRequestInfo().Subject.ToString());

        return csr;
    }
}
