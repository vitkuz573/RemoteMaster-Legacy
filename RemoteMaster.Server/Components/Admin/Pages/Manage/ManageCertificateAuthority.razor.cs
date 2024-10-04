// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Security.Cryptography.X509Certificates;
using Microsoft.JSInterop;
using Serilog;

namespace RemoteMaster.Server.Components.Admin.Pages.Manage;

public partial class ManageCertificateAuthority
{
    private string _subject = string.Empty;
    private int _keySize;
    private string _serialNumber = string.Empty;
    private string _signatureAlgorithm = string.Empty;
    private DateTime _issueDate;
    private DateTime _expiryDate;
    private string _exportPassword = string.Empty;

    protected override void OnInitialized()
    {
        var caCertificateResult = CertificateAuthorityService.GetCaCertificate(X509ContentType.Pfx);

        if (caCertificateResult.IsSuccess)
        {
            var caCertificate = caCertificateResult.Value;
            var caPrivateKey = caCertificate.GetRSAPrivateKey();

            _subject = caCertificate.Subject;
            _keySize = caPrivateKey.KeySize;
            _serialNumber = caCertificate.SerialNumber;
            _signatureAlgorithm = caCertificate.SignatureAlgorithm.FriendlyName;
            _issueDate = caCertificate.NotBefore;
            _expiryDate = caCertificate.NotAfter;
        }
        else
        {
            Log.Error($"Error retrieving CA certificate: {caCertificateResult.Errors.First().Message}");
        }
    }

    private async Task ExportCaCertificateAsync()
    {
        try
        {
            var caCertificateResult = CertificateAuthorityService.GetCaCertificate(X509ContentType.Pfx);

            if (caCertificateResult.IsSuccess)
            {
                var caCertificate = caCertificateResult.Value;

                if (caCertificate.HasPrivateKey)
                {
                    var pfxBytes = caCertificate.Export(X509ContentType.Pfx, _exportPassword);
                    var base64Pfx = Convert.ToBase64String(pfxBytes);

                    var module = await JsRuntime.InvokeAsync<IJSObjectReference>("import", "./js/fileUtils.js");

                    await module.InvokeVoidAsync("downloadDataAsFile", base64Pfx, $"{_subject}.pfx", "application/x-pkcs12;base64");
                }
                else
                {
                    throw new InvalidOperationException("The certificate does not contain a private key.");
                }
            }
            else
            {
                Log.Error($"Error retrieving CA certificate for export: {caCertificateResult.Errors.First().Message}");
            }
        }
        catch (Exception ex)
        {
            Log.Error($"Error exporting certificate: {ex.Message}");

            throw;
        }
    }
}
