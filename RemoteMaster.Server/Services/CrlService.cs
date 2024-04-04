// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Numerics;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using RemoteMaster.Server.Abstractions;
using RemoteMaster.Server.Data;
using RemoteMaster.Server.Models;
using Serilog;

namespace RemoteMaster.Server.Services;

public class CrlService(IOptions<CertificateOptions> options, IDbContextFactory<CertificateDbContext> contextFactory) : ICrlService
{
    private readonly CertificateOptions _settings = options.Value;
    
    public async Task RevokeCertificateAsync(string serialNumber, X509RevocationReason reason)
    {
        var revokedCertificate = new RevokedCertificate
        {
            SerialNumber = serialNumber,
            Reason = reason.ToString(),
            RevocationDate = DateTime.UtcNow
        };

        using var context = await contextFactory.CreateDbContextAsync();

        context.RevokedCertificates.Add(revokedCertificate);
        context.SaveChanges();
    }

    public async Task<byte[]> GenerateCrlAsync()
    {
        var issuerCertificate = GetIssuerCertificate();
        var crlBuilder = new CertificateRevocationListBuilder();

        using var context = await contextFactory.CreateDbContextAsync();

        var crlInfo = context.CrlInfos.FirstOrDefault() ?? new CrlInfo
        {
            CrlNumber = BigInteger.Zero.ToString()
        };

        var currentCrlNumber = BigInteger.Parse(crlInfo.CrlNumber) + 1;
        var nextUpdate = DateTimeOffset.UtcNow.AddDays(30);

        var revokedCertificates = context.RevokedCertificates.ToList();

        foreach (var revoked in revokedCertificates)
        {
            var serialNumberBytes = Enumerable.Range(0, revoked.SerialNumber.Length)
                .Where(x => x % 2 == 0)
                .Select(x => Convert.ToByte(revoked.SerialNumber.Substring(x, 2), 16))
                .ToArray();

            if (!Enum.TryParse<X509RevocationReason>(revoked.Reason, out var reason))
            {
                Log.Warning("Failed to parse the certificate revocation reason: '{Reason}'. Using default value 'Unspecified'.", revoked.Reason);
                
                reason = X509RevocationReason.Unspecified;
            }

            crlBuilder.AddEntry(serialNumberBytes, revoked.RevocationDate, reason);
        }

        var crlData = crlBuilder.Build(issuerCertificate, currentCrlNumber, nextUpdate, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        var crlHash = BitConverter.ToString(SHA256.HashData(crlData)).Replace("-", "").ToLowerInvariant();

        crlInfo.CrlNumber = currentCrlNumber.ToString();
        crlInfo.NextUpdate = nextUpdate;
        crlInfo.CrlHash = crlHash;

        if (context.CrlInfos.Any())
        {
            context.CrlInfos.Update(crlInfo);
        }
        else
        {
            context.CrlInfos.Add(crlInfo);
        }

        context.SaveChanges();

        return crlData;
    }

    public async Task<bool> PublishCrlAsync(byte[] crlData)
    {
        try
        {
            var programDataPath = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
            var crlFilePath = Path.Combine(programDataPath, "RemoteMaster", "list.crl");

            var directoryPath = Path.GetDirectoryName(crlFilePath);

            if (!Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }

            await File.WriteAllBytesAsync(crlFilePath, crlData);

            Log.Information($"CRL published to {crlFilePath}");

            return true;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to publish CRL");

            return false;
        }
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

    public async Task<CrlMetadata> GetCrlMetadataAsync()
    {
        using var context = await contextFactory.CreateDbContextAsync();

        var crlInfo = await context.CrlInfos.FirstOrDefaultAsync();
        var revokedCertificatesCount = await context.RevokedCertificates.CountAsync();

        if (crlInfo != null)
        {
            return new CrlMetadata
            {
                CrlInfo = crlInfo,
                RevokedCertificatesCount = revokedCertificatesCount
            };
        }

        throw new InvalidOperationException("CRL Metadata is not available.");
    }
}
