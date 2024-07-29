// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using Microsoft.JSInterop;

namespace RemoteMaster.Server.Components.Admin.Pages.Manage;

public partial class ManageJwt
{
    private string? _keysDirectory;
    private int? _keySize;
    private string? _newKeyPassword;

    protected override void OnInitialized()
    {
        var jwt = Jwt.Value;

        _keysDirectory = jwt.KeysDirectory;
        _keySize = jwt.KeySize;
    }

    private async Task ExportKeysAsync()
    {
        var memoryStream = new MemoryStream();

        using (var archive = new ZipArchive(memoryStream, ZipArchiveMode.Create, true))
        {
            var files = new string[] { "private_key.der", "public_key.der" };

            foreach (var file in files)
            {
                var filePath = Path.Combine(_keysDirectory, file);
                var entry = archive.CreateEntry(file, CompressionLevel.Fastest);

                await using var fileStream = new FileStream(filePath, FileMode.Open);
                await using var entryStream = entry.Open();

                await fileStream.CopyToAsync(entryStream);
            }
        }
        memoryStream.Position = 0;

        var base64Zip = Convert.ToBase64String(memoryStream.ToArray());

        var module = await JsRuntime.InvokeAsync<IJSObjectReference>("import", "./js/fileUtils.js");

        await module.InvokeVoidAsync("downloadDataAsFile", base64Zip, "jwtKeys.zip", "application/zip;base64");
    }

    private async Task ChangePasswordAsync()
    {
        if (string.IsNullOrWhiteSpace(_newKeyPassword))
        {
            await JsRuntime.InvokeVoidAsync("alert", "New password cannot be empty.");
            return;
        }

        var privateKeyPath = Path.Combine(_keysDirectory, "private_key.der");

        try
        {
            // Load the existing private key
            var privateKeyBytes = await File.ReadAllBytesAsync(privateKeyPath);
            using var rsa = RSA.Create();
            rsa.ImportEncryptedPkcs8PrivateKey(Encoding.UTF8.GetBytes(Jwt.Value.KeyPassword), privateKeyBytes, out _);

            // Encrypt with new password
            var newKeyPasswordBytes = Encoding.UTF8.GetBytes(_newKeyPassword);
            var encryptionAlgorithm = new PbeParameters(PbeEncryptionAlgorithm.Aes256Cbc, HashAlgorithmName.SHA256, 100000);

            await File.WriteAllBytesAsync(privateKeyPath, rsa.ExportEncryptedPkcs8PrivateKey(newKeyPasswordBytes, encryptionAlgorithm));

            await JsRuntime.InvokeVoidAsync("alert", "Key password changed successfully.");
        }
        catch (Exception ex)
        {
            await JsRuntime.InvokeVoidAsync("alert", $"Failed to change key password: {ex.Message}");
        }
    }
}
