// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.IO.Abstractions;
using System.Numerics;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using FluentResults;
using RemoteMaster.Server.Abstractions;
using RemoteMaster.Server.Entities;
using RemoteMaster.Server.ValueObjects;
using Serilog;

namespace RemoteMaster.Server.Services;

public class CrlService(ICrlRepository crlRepository, ICertificateProvider certificateProvider, IFileSystem fileSystem) : ICrlService
{
    public async Task<Result> RevokeCertificateAsync(SerialNumber serialNumber, X509RevocationReason reason)
    {
        try
        {
            var crl = (await crlRepository.GetAllAsync()).FirstOrDefault() ?? new Crl(BigInteger.Zero.ToString());

            try
            {
                crl.RevokeCertificate(serialNumber, reason);
            }
            catch (InvalidOperationException ex)
            {
                Log.Information($"Certificate with serial number {serialNumber} has already been revoked.");
                
                return Result.Fail(ex.Message);
            }

            if (crl.Id > 0)
            {
                await crlRepository.UpdateAsync(crl);
            }
            else
            {
                await crlRepository.AddAsync(crl);
            }

            await crlRepository.SaveChangesAsync();

            Log.Information($"Certificate with serial number {serialNumber} has been successfully revoked.");

            return Result.Ok();
        }
        catch (Exception ex)
        {
            Log.Error(ex, $"Error revoking certificate with serial number {serialNumber}");
            
            return Result.Fail($"Error revoking certificate with serial number {serialNumber}").WithError(ex.Message);
        }
    }

    public async Task<Result<byte[]>> GenerateCrlAsync()
    {
        try
        {
            var issuerCertificateResult = certificateProvider.GetIssuerCertificate();

            if (issuerCertificateResult.IsFailed)
            {
                return Result.Fail<byte[]>("Error retrieving issuer certificate.")
                             .WithError(issuerCertificateResult.Errors.FirstOrDefault()?.Message);
            }

            var issuerCertificate = issuerCertificateResult.Value;
            var crlBuilder = new CertificateRevocationListBuilder();

            var crl = (await crlRepository.GetAllAsync()).MinBy(ci => ci.Number) ?? new Crl(BigInteger.Zero.ToString());

            var currentCrlNumber = BigInteger.Parse(crl.Number) + 1;
            var nextUpdate = DateTimeOffset.UtcNow.AddDays(30);

            var revokedCertificates = crl.RevokedCertificates;

            foreach (var revoked in revokedCertificates)
            {
                var serialNumberBytes = revoked.SerialNumber.ToByteArray();

                crlBuilder.AddEntry(serialNumberBytes, revoked.RevocationDate, revoked.Reason);
            }

            var crlData = crlBuilder.Build(issuerCertificate, currentCrlNumber, nextUpdate, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);

            crl.SetNumber(currentCrlNumber.ToString());

            if (crl.Id > 0)
            {
                await crlRepository.UpdateAsync(crl);
            }
            else
            {
                await crlRepository.AddAsync(crl);
            }

            await crlRepository.SaveChangesAsync();

            return Result.Ok(crlData);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error generating CRL");

            return Result.Fail<byte[]>("Error generating CRL").WithError(ex.Message);
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

            return Result.Ok();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to publish CRL");

            return Result.Fail("Failed to publish CRL").WithError(ex.Message);
        }
    }
}
