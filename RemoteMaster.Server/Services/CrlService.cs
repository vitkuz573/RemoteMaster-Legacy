// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Numerics;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Extensions.Options;
using RemoteMaster.Server.Abstractions;
using RemoteMaster.Server.Data;
using RemoteMaster.Server.Models;
using Serilog;

namespace RemoteMaster.Server.Services;

public class CrlService(IOptions<CertificateOptions> options, CertificateDbContext context) : ICrlService
{
    private readonly CertificateOptions _settings = options.Value;
    
    public void RevokeCertificate(string serialNumber, X509RevocationReason reason)
    {
        var revokedCertificate = new RevokedCertificate
        {
            SerialNumber = serialNumber,
            Reason = reason.ToString(),
            RevocationDate = DateTime.UtcNow
        };

        context.RevokedCertificates.Add(revokedCertificate);
        context.SaveChanges();
    }

    public byte[] GenerateCrl()
    {
        var issuerCertificate = GetIssuerCertificate();
        var crlBuilder = new CertificateRevocationListBuilder();

        var crlInfo = context.CrlInfos.FirstOrDefault() ?? new CrlInfo
        {
            CurrentCrlNumber = BigInteger.Zero.ToString()
        };

        var currentCrlNumber = BigInteger.Parse(crlInfo.CurrentCrlNumber);

        var revokedCertificates = context.RevokedCertificates.ToList();

        foreach (var revoked in revokedCertificates)
        {
            var serialNumberBytes = Enumerable.Range(0, revoked.SerialNumber.Length)
                                 .Where(x => x % 2 == 0)
                                 .Select(x => Convert.ToByte(revoked.SerialNumber.Substring(x, 2), 16))
                                 .ToArray();

            Enum.TryParse<X509RevocationReason>(revoked.Reason, out var reason);

            crlBuilder.AddEntry(serialNumberBytes, revoked.RevocationDate, reason);
        }

        currentCrlNumber += 1;

        crlInfo.CurrentCrlNumber = currentCrlNumber.ToString();

        if (context.CrlInfos.Any())
        {
            context.CrlInfos.Update(crlInfo);
        }
        else
        {
            context.CrlInfos.Add(crlInfo);
        }

        context.SaveChanges();

        var nextUpdate = DateTimeOffset.UtcNow.AddDays(30);
        var crlData = crlBuilder.Build(issuerCertificate, currentCrlNumber, nextUpdate, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);

        return crlData;
    }

    public void PublishCrl(byte[] crlData)
    {
        var programDataPath = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
        var crlFilePath = Path.Combine(programDataPath, "RemoteMaster", "list.crl");

        var directoryPath = Path.GetDirectoryName(crlFilePath);

        if (!Directory.Exists(directoryPath))
        {
            Directory.CreateDirectory(directoryPath);
        }

        File.WriteAllBytes(crlFilePath, crlData);

        Log.Information($"CRL published to {crlFilePath}");
    }

    private X509Certificate2 GetIssuerCertificate()
    {
        X509Certificate2? caCertificate = null;

        using (var store = new X509Store(StoreName.Root, StoreLocation.LocalMachine))
        {
            store.Open(OpenFlags.ReadOnly);
            
            var certificates = store.Certificates.Find(X509FindType.FindBySubjectName, _settings.CommonName, false);

            foreach (var cert in certificates)
            {
                if (cert.HasPrivateKey)
                {
                    caCertificate = cert;
                    break;
                }
            }
        }

        if (caCertificate == null)
        {
            throw new InvalidOperationException($"CA certificate with CommonName '{_settings.CommonName}' not found.");
        }

        return caCertificate;
    }
}
