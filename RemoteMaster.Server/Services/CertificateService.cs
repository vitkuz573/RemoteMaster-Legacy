// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Security.Cryptography;
using Microsoft.Extensions.Options;
using Org.BouncyCastle.Asn1;
using Org.BouncyCastle.Asn1.Pkcs;
using Org.BouncyCastle.Asn1.X509;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Operators;
using Org.BouncyCastle.Math;
using Org.BouncyCastle.Pkcs;
using Org.BouncyCastle.X509;
using RemoteMaster.Server.Abstractions;
using RemoteMaster.Server.Models;

namespace RemoteMaster.Server.Services;

public class CertificateService : ICertificateService
{
    private readonly CertificateSettings _settings;

    public CertificateService(IOptions<CertificateSettings> options)
    {
        if (options == null)
        {
            throw new ArgumentNullException(nameof(options));
        }

        _settings = options.Value;
    }

    private AsymmetricKeyParameter GetCAPrivateKey()
    {
        using var fs = new FileStream(_settings.PfxPath, FileMode.Open, FileAccess.Read);
        var pfxStore = new Pkcs12StoreBuilder().Build();
        pfxStore.Load(fs, _settings.PfxPassword.ToCharArray());

        string alias = null;

        foreach (var al in pfxStore.Aliases)
        {
            if (pfxStore.IsKeyEntry(al) && pfxStore.GetKey(al).Key.IsPrivate)
            {
                alias = al;
                break;
            }
        }

        if (alias == null)
        {
            throw new Exception("No private key found in PFX");
        }

        return pfxStore.GetKey(alias).Key;
    }

    private X509Name GetCACertificateIssuerDN()
    {
        using var fs = new FileStream(_settings.PfxPath, FileMode.Open, FileAccess.Read);
        var pfxStore = new Pkcs12StoreBuilder().Build();
        pfxStore.Load(fs, _settings.PfxPassword.ToCharArray());

        foreach (var alias in pfxStore.Aliases)
        {
            if (pfxStore.IsCertificateEntry(alias))
            {
                var certificateEntry = pfxStore.GetCertificate(alias);
                return certificateEntry.Certificate.IssuerDN;
            }
        }

        throw new Exception("No CA certificate found in PFX");
    }

    private static ISignatureFactory GetSignatureFactory(AsymmetricKeyParameter privateKey)
    {
        return new Asn1SignatureFactory("SHA256WITHRSA", privateKey);
    }

    public X509Certificate GenerateCertificateFromCSR(Pkcs10CertificationRequest csr)
    {
        if (csr == null)
        {
            throw new ArgumentNullException(nameof(csr));
        }

        var caPrivateKey = GetCAPrivateKey();
        var signatureFactory = GetSignatureFactory(caPrivateKey);

        // Generate the certificate using the CSR
        var certGenerator = new X509V3CertificateGenerator();
        certGenerator.SetSerialNumber(GenerateSerialNumber());
        certGenerator.SetIssuerDN(GetCACertificateIssuerDN());
        certGenerator.SetNotBefore(DateTime.UtcNow);
        certGenerator.SetNotAfter(DateTime.UtcNow.AddYears(1));
        certGenerator.SetSubjectDN(csr.GetCertificationRequestInfo().Subject);
        certGenerator.SetPublicKey(csr.GetPublicKey());

        var csrAttributes = csr.GetCertificationRequestInfo().Attributes;

        foreach (var attr in csrAttributes)
        {
            if (attr.ToAsn1Object() is Asn1Sequence seq)
            {
                var attrPkcs = AttributePkcs.GetInstance(seq);

                if (attrPkcs.AttrType.Equals(PkcsObjectIdentifiers.Pkcs9AtExtensionRequest))
                {
                    var extensions = X509Extensions.GetInstance((Asn1Sequence)attrPkcs.AttrValues[0]);
                    var sanExtension = extensions.GetExtension(X509Extensions.SubjectAlternativeName);

                    if (sanExtension != null)
                    {
                        certGenerator.AddExtension(X509Extensions.SubjectAlternativeName, sanExtension.IsCritical, sanExtension.GetParsedValue());
                    }
                }
            }
        }

        // Sign the certificate with the CA's private key
        return certGenerator.Generate(signatureFactory);
    }

    private static BigInteger GenerateSerialNumber()
    {
        var serialNumberBytes = new byte[16];

        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(serialNumberBytes);
        }

        var serialNumber = new BigInteger(1, serialNumberBytes);

        return serialNumber;
    }
}