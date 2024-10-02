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
        sb.AppendLine("Details of the SSL certificate errors:<br>");

        if (sslErrors.HasFlag(SslPolicyErrors.RemoteCertificateChainErrors))
        {
            sb.AppendLine("- <strong>Certificate chain errors:</strong> This could mean that the certificate was issued by an untrusted certification authority, or it has expired.<br>");
        }

        if (sslErrors.HasFlag(SslPolicyErrors.RemoteCertificateNameMismatch))
        {
            sb.AppendLine("- <strong>Certificate name mismatch:</strong> The domain name does not match the name on the certificate.<br>");
        }

        if (sslErrors.HasFlag(SslPolicyErrors.RemoteCertificateNotAvailable))
        {
            sb.AppendLine("- <strong>Certificate not available:</strong> The certificate could not be retrieved from the remote server.<br>");
        }

        sb.AppendLine("<br>These issues could pose a security risk. Do you want to proceed with the connection?");

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
