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
        sb.AppendLine("<strong>Critical Warning:</strong> Severe SSL certificate issues have been detected during the connection attempt.<br><br>");
        sb.AppendLine("The following SSL certificate errors were identified:<br>");

        if (sslErrors.HasFlag(SslPolicyErrors.RemoteCertificateChainErrors))
        {
            sb.AppendLine("- <strong>Certificate chain integrity compromised:</strong> The certificate chain is either incomplete, expired, or issued by an untrusted authority. This represents a serious risk as the server's identity cannot be reliably verified.<br>");
        }

        if (sslErrors.HasFlag(SslPolicyErrors.RemoteCertificateNameMismatch))
        {
            sb.AppendLine("- <strong>Domain mismatch:</strong> The domain name presented by the server does not match the name on the certificate. This is a critical security vulnerability that could indicate a man-in-the-middle attack or server misconfiguration.<br>");
        }

        if (sslErrors.HasFlag(SslPolicyErrors.RemoteCertificateNotAvailable))
        {
            sb.AppendLine("- <strong>SSL certificate unavailable:</strong> The server's SSL certificate could not be retrieved. This may signal a severe server misconfiguration, network tampering, or even malicious interference.<br>");
        }

        sb.AppendLine("<br><strong>Immediate action required:</strong> These SSL issues represent a significant threat to the security of your connection. Sensitive data may be exposed, and the integrity of the server cannot be assured. It is strongly recommended that you do <strong>not</strong> proceed unless you fully understand the risks involved and have taken additional security measures.<br>");
        sb.AppendLine("Do you still wish to proceed despite these critical security warnings?");

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
