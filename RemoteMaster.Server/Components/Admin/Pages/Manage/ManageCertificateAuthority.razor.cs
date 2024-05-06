// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Security.Cryptography.X509Certificates;
using Microsoft.JSInterop;
using Serilog;

namespace RemoteMaster.Server.Components.Admin.Pages.Manage;

public partial class ManageCertificateAuthority
{
    private string _commonName;
    private int _keySize;
    private int _validityPeriod;
    private string _serialNumber;
    private string _signatureAlgorithm;
    private DateTime _issueDate;
    private DateTime _expiryDate;
    private string _exportPassword;

    protected override void OnInitialized()
    {
        var certificateAuthorityCertificate = CertificateAuthorityCertificate.Value;

        _commonName = certificateAuthorityCertificate.CommonName;
        _keySize = certificateAuthorityCertificate.KeySize;
        _validityPeriod = certificateAuthorityCertificate.ValidityPeriod;

        var caCertificate = CertificateService.GetPrivateCaCertificate();

        _serialNumber = caCertificate.SerialNumber;
        _signatureAlgorithm = caCertificate.SignatureAlgorithm.FriendlyName;
        _issueDate = caCertificate.NotBefore;
        _expiryDate = caCertificate.NotAfter;
    }

    private async Task ExportCaCertificateAsync()
    {
        try
        {
            var caCertificate = CertificateService.GetPrivateCaCertificate();

            if (caCertificate.HasPrivateKey)
            {
                var pfxBytes = caCertificate.Export(X509ContentType.Pfx, _exportPassword);
                var base64Pfx = Convert.ToBase64String(pfxBytes);

                await JsRuntime.InvokeVoidAsync("saveAsFile", $"{_commonName}.pfx", base64Pfx);
            }
            else
            {
                throw new InvalidOperationException("The certificate does not contain a private key.");
            }
        }
        catch (Exception ex)
        {
            Log.Error($"Error exporting certificate: {ex.Message}");
        }
    }
}
