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
        sb.AppendLine("<strong>Warning:</strong> SSL certificate issues detected during the connection attempt.<br><br>");
        sb.AppendLine("The following SSL errors were found:<br><ul>");

        if (sslErrors.HasFlag(SslPolicyErrors.RemoteCertificateChainErrors))
        {
            sb.AppendLine("<li><strong>Certificate chain issue:</strong> The certificate chain is incomplete, expired, or from an untrusted authority. This means the server's identity may not be reliable.</li>");
        }

        if (sslErrors.HasFlag(SslPolicyErrors.RemoteCertificateNameMismatch))
        {
            sb.AppendLine("<li><strong>Name mismatch:</strong> The server's domain does not match the certificate. This could indicate a man-in-the-middle attack or a server misconfiguration.</li>");
        }

        if (sslErrors.HasFlag(SslPolicyErrors.RemoteCertificateNotAvailable))
        {
            sb.AppendLine("<li><strong>Certificate unavailable:</strong> The server's SSL certificate could not be retrieved. This may suggest a misconfiguration or malicious activity.</li>");
        }

        sb.AppendLine("</ul>");
        sb.AppendLine("<br><strong>Proceed with caution:</strong> These SSL issues could compromise the security of your connection. Sensitive data may be exposed, and server integrity is not guaranteed. Proceed only if you fully understand the risks.<br>");
        sb.AppendLine("Do you wish to continue despite these warnings?");

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
