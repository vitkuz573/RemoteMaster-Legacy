// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.IO.Compression;
using Microsoft.JSInterop;

namespace RemoteMaster.Server.Components.Admin.Pages.Manage;

public partial class ManageJwt
{
    private string _keysDirectory;
    private int _keySize;

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
                
                using var fileStream = new FileStream(filePath, FileMode.Open);
                using var entryStream = entry.Open();
                
                await fileStream.CopyToAsync(entryStream);
            }
        }
        memoryStream.Position = 0;
        
        await JsRuntime.InvokeVoidAsync("saveAsFile", "jwtKeys.zip", Convert.ToBase64String(memoryStream.ToArray()));
    }
}
