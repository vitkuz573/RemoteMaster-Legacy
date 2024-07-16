// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.IO.Abstractions;
using System.Numerics;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Microsoft.EntityFrameworkCore;
using RemoteMaster.Server.Abstractions;
using RemoteMaster.Server.Data;
using RemoteMaster.Server.Models;
using Serilog;

namespace RemoteMaster.Server.Services;

public class CrlService(IDbContextFactory<CertificateDbContext> contextFactory, ICertificateProvider certificateProvider, IFileSystem fileSystem) : ICrlService
{
    public async Task RevokeCertificateAsync(string serialNumber, X509RevocationReason reason)
    {
        await using var context = await contextFactory.CreateDbContextAsync();

        var existingRevokedCertificate = await context.RevokedCertificates.FirstOrDefaultAsync(rc => rc.SerialNumber == serialNumber);

        if (existingRevokedCertificate != null)
        {
            Log.Information($"Certificate with serial number {serialNumber} has already been revoked.");

            return;
        }

        var revokedCertificate = new RevokedCertificate
        {
            SerialNumber = serialNumber,
            Reason = reason,
            RevocationDate = DateTimeOffset.UtcNow
        };

        context.RevokedCertificates.Add(revokedCertificate);
        await context.SaveChangesAsync();

        Log.Information($"Certificate with serial number {serialNumber} has been successfully revoked.");
    }

    public async Task<byte[]> GenerateCrlAsync()
    {
        var issuerCertificate = certificateProvider.GetIssuerCertificate();
        var crlBuilder = new CertificateRevocationListBuilder();

        await using var context = await contextFactory.CreateDbContextAsync();

        var crlInfo = context.CrlInfos.OrderBy(ci => ci.CrlNumber).FirstOrDefault() ?? new CrlInfo
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

            crlBuilder.AddEntry(serialNumberBytes, revoked.RevocationDate, revoked.Reason);
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

        await context.SaveChangesAsync();

        return crlData;
    }

    public async Task<bool> PublishCrlAsync(byte[] crlData, string? customPath = null)
    {
        try
        {
            var programDataPath = customPath ?? Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
            var crlFilePath = fileSystem.Path.Combine(programDataPath, "RemoteMaster", "list.crl");

            var directoryPath = fileSystem.Path.GetDirectoryName(crlFilePath);

            if (string.IsNullOrEmpty(directoryPath))
            {
                throw new InvalidOperationException("Directory path is null or empty.");
            }

            if (!fileSystem.Directory.Exists(directoryPath))
            {
                fileSystem.Directory.CreateDirectory(directoryPath);
            }

            await fileSystem.File.WriteAllBytesAsync(crlFilePath, crlData);

            Log.Information($"CRL published to {crlFilePath}");

            return true;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to publish CRL");

            return false;
        }
    }

    public async Task<CrlMetadata> GetCrlMetadataAsync()
    {
        await using var context = await contextFactory.CreateDbContextAsync();

        var crlInfo = await context.CrlInfos.OrderBy(ci => ci.CrlNumber).FirstOrDefaultAsync();
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
