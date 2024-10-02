// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Net.Security;
using System.Text;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace RemoteMaster.Server.Components.Dialogs;

public partial class SslWarningDialog
{
    [CascadingParameter]
    private MudDialogInstance MudDialog { get; set; } = default!;

    [Parameter]
    public SslPolicyErrors SslPolicyErrors { get; set; }

    private MarkupString _contentText;

    protected override void OnInitialized()
    {
        _contentText = BuildWarningMessage(SslPolicyErrors);
    }

    private static MarkupString BuildWarningMessage(SslPolicyErrors sslErrors)
    {
        var sb = new StringBuilder();
        sb.AppendLine("<strong>Warning:</strong> SSL certificate issues have been detected during the connection attempt.<br><br>");
        sb.AppendLine("The following SSL certificate errors were encountered:<br>");

        if (sslErrors.HasFlag(SslPolicyErrors.RemoteCertificateChainErrors))
        {
            sb.AppendLine("- <strong>Certificate chain errors:</strong> This indicates that the certificate chain cannot be verified. This could be caused by an untrusted certificate authority, an expired certificate, or missing intermediate certificates.<br>");
        }

        if (sslErrors.HasFlag(SslPolicyErrors.RemoteCertificateNameMismatch))
        {
            sb.AppendLine("- <strong>Certificate name mismatch:</strong> The domain name presented by the server does not match the name on the SSL certificate. This could be a sign of a misconfigured server or a potential man-in-the-middle attack.<br>");
        }

        if (sslErrors.HasFlag(SslPolicyErrors.RemoteCertificateNotAvailable))
        {
            sb.AppendLine("- <strong>Certificate not available:</strong> The SSL certificate could not be retrieved from the remote server, potentially due to a misconfiguration or network issue.<br>");
        }

        sb.AppendLine("<br><strong>These issues pose a significant security risk.</strong> Proceeding with the connection could expose sensitive data or lead to malicious interception. Do you want to continue with the connection despite these risks?");

        return new MarkupString(sb.ToString());
    }

    private void Discard()
    {
        MudDialog.Cancel();
    }

    private void Continue()
    {
        MudDialog.Close(DialogResult.Ok(string.Empty));
    }
}
