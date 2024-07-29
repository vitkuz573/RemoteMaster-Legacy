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
using RemoteMaster.Server.DTOs;
using RemoteMaster.Server.Entities;
using RemoteMaster.Server.Models;
using RemoteMaster.Shared.Models;
using Serilog;

namespace RemoteMaster.Server.Services;

public class CrlService(IDbContextFactory<CertificateDbContext> contextFactory, ICertificateProvider certificateProvider, IFileSystem fileSystem) : ICrlService
{
    public async Task<Result> RevokeCertificateAsync(string serialNumber, X509RevocationReason reason)
    {
        try
        {
            var context = await contextFactory.CreateDbContextAsync();

            var existingRevokedCertificate = await context.RevokedCertificates.FirstOrDefaultAsync(rc => rc.SerialNumber == serialNumber);

            if (existingRevokedCertificate != null)
            {
                Log.Information($"Certificate with serial number {serialNumber} has already been revoked.");

                return Result.Success();
            }

            var revokedCertificate = new RevokedCertificate
            {
                SerialNumber = serialNumber,
                Reason = reason,
                RevocationDate = DateTimeOffset.UtcNow
            };

            context.RevokedCertificates.Add(revokedCertificate);

            var result = await context.SaveChangesAsync();

            if (result > 0)
            {
                Log.Information($"Certificate with serial number {serialNumber} has been successfully revoked.");
                
                return Result.Success();
            }

            Log.Error($"Failed to revoke certificate with serial number {serialNumber}.");
                
            return Result.Failure($"Failed to revoke certificate with serial number {serialNumber}.");
        }
        catch (Exception ex)
        {
            Log.Error(ex, $"Error revoking certificate with serial number {serialNumber}");
            
            return Result.Failure($"Error revoking certificate with serial number {serialNumber}", exception: ex);
        }
    }

    public async Task<Result<byte[]>> GenerateCrlAsync()
    {
        try
        {
            var issuerCertificateResult = certificateProvider.GetIssuerCertificate();

            if (!issuerCertificateResult.IsSuccess)
            {
                return Result<byte[]>.Failure("Error retrieving issuer certificate.", exception: issuerCertificateResult.Errors.FirstOrDefault()?.Exception);
            }

            var issuerCertificate = issuerCertificateResult.Value;

            var crlBuilder = new CertificateRevocationListBuilder();

            var context = await contextFactory.CreateDbContextAsync();

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

            return Result<byte[]>.Success(crlData);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error generating CRL");

            return Result<byte[]>.Failure("Error generating CRL", exception: ex);
        }
    }

    public async Task<Result> PublishCrlAsync(byte[] crlData, string? customPath = null)
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

            return Result.Success();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to publish CRL");
            
            return Result.Failure("Failed to publish CRL", exception: ex);
        }
    }

    public async Task<Result<CrlMetadata>> GetCrlMetadataAsync()
    {
        try
        {
            var context = await contextFactory.CreateDbContextAsync();

            var crlInfoDto = await context.CrlInfos
                .OrderBy(ci => ci.CrlNumber)
                .Select(ci => new CrlInfoDto
                {
                    CrlNumber = ci.CrlNumber,
                    NextUpdate = ci.NextUpdate,
                    CrlHash = ci.CrlHash
                })
                .FirstOrDefaultAsync();

            var revokedCertificatesCount = await context.RevokedCertificates.CountAsync();

            if (crlInfoDto == null)
            {
                return Result<CrlMetadata>.Failure("CRL Metadata is not available.");
            }

            var metadata = new CrlMetadata(crlInfoDto, revokedCertificatesCount);

            return Result<CrlMetadata>.Success(metadata);

        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error retrieving CRL metadata");

            return Result<CrlMetadata>.Failure("Error retrieving CRL metadata", exception: ex);
        }
    }
}
