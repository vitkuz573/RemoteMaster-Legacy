// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Net;
using System.Net.Security;
using System.Text;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using RemoteMaster.Server.Models;

namespace RemoteMaster.Server.Components.Dialogs;

public partial class SslWarningDialog
{
    [CascadingParameter]
    private MudDialogInstance MudDialog { get; set; } = default!;

    [Parameter]
    public IPAddress IpAddress { get; set; } = default!;

    [Parameter]
    public SslPolicyErrors SslPolicyErrors { get; set; }

    [Parameter]
    public CertificateInfo CertificateInfo { get; set; } = default!;

    private MarkupString _contentText;

    protected override void OnInitialized()
    {
        _contentText = BuildWarningMessage();
    }

    private MarkupString BuildWarningMessage()
    {
        var sb = new StringBuilder();
        sb.AppendLine($"<strong>Warning:</strong> SSL certificate issues detected during the connection attempt to IP address <strong>{IpAddress}</strong>.<br><br>");
        sb.AppendLine("The following SSL errors were found:<br><ul>");

        if (SslPolicyErrors.HasFlag(SslPolicyErrors.RemoteCertificateChainErrors))
        {
            sb.AppendLine("<li><strong>Certificate chain issue:</strong> The certificate chain is incomplete, expired, or from an untrusted authority. This means the server's identity may not be reliable.</li>");
        }

        if (SslPolicyErrors.HasFlag(SslPolicyErrors.RemoteCertificateNameMismatch))
        {
            sb.AppendLine("<li><strong>Name mismatch:</strong> The server's domain does not match the certificate. This could indicate a man-in-the-middle attack or a server misconfiguration.</li>");
        }

        if (SslPolicyErrors.HasFlag(SslPolicyErrors.RemoteCertificateNotAvailable))
        {
            sb.AppendLine("<li><strong>Certificate unavailable:</strong> The server's SSL certificate could not be retrieved. This may suggest a misconfiguration or malicious activity.</li>");
            sb.AppendLine("<li><strong>Warning:</strong> No SSL certificate means the connection is <strong>not secure</strong>. Data could be intercepted or modified by third parties.</li>");
        }

        sb.AppendLine("</ul>");

        if (!SslPolicyErrors.HasFlag(SslPolicyErrors.RemoteCertificateNotAvailable))
        {
            sb.AppendLine("<br><strong>Certificate Details:</strong><br>");
            sb.AppendLine($"<strong>Issuer:</strong> {CertificateInfo.Issuer}<br>");
            sb.AppendLine($"<strong>Subject:</strong> {CertificateInfo.Subject}<br>");
            sb.AppendLine($"<strong>Valid From:</strong> {CertificateInfo.EffectiveDate}<br>");
            sb.AppendLine($"<strong>Valid Until:</strong> {CertificateInfo.ExpirationDate}<br>");
            sb.AppendLine($"<strong>Signature Algorithm:</strong> {CertificateInfo.SignatureAlgorithm}<br>");
            sb.AppendLine($"<strong>Key Size:</strong> {CertificateInfo.KeySize} bits<br><br>");

            if (CertificateInfo.CertificateChain.Any())
            {
                sb.AppendLine("<strong>Certificate Chain:</strong><br>");
                sb.AppendLine("<ul>");

                foreach (var chainElement in CertificateInfo.CertificateChain)
                {
                    sb.AppendLine($"<li>{chainElement}</li>");
                }

                sb.AppendLine("</ul><br>");
            }
        }

        sb.AppendLine("<strong>Proceed with caution:</strong> These SSL issues could compromise the security of your connection. Sensitive data may be exposed, and server integrity is not guaranteed. Proceed only if you fully understand the risks.<br>");
        sb.AppendLine("Do you wish to continue despite these warnings?");

        return new MarkupString(sb.ToString());
    }

    private void Discard()
    {
        MudDialog.Cancel();
    }

    private void Continue()
    {
        SslWarningService.SetSslAllowance(IpAddress, true);
        
        MudDialog.Close(DialogResult.Ok(string.Empty));
    }
}
