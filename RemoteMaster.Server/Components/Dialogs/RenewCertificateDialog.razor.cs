// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNetCore.SignalR.Client;

namespace RemoteMaster.Server.Components.Dialogs;

public partial class RenewCertificateDialog
{
    private readonly List<X509RevocationReason> revocationReasons = Enum.GetValues(typeof(X509RevocationReason))
        .Cast<X509RevocationReason>()
        .Where(reason => reason != X509RevocationReason.AACompromise && reason != X509RevocationReason.RemoveFromCrl)
        .ToList();

    private X509RevocationReason _selectedRevocationReason = X509RevocationReason.Superseded;

    private async Task Confirm()
    {
        var revokeCertificateTasks = new List<Task>();
        var renewCertificateTasks = new List<Task>();

        foreach (var (computer, connection) in Hosts)
        {
            var serialNumber = await connection.InvokeAsync<string>("GetCertificateSerialNumber");

            var revokeCertificateTask = Task.Run(async () => await CrlService.RevokeCertificateAsync(serialNumber, _selectedRevocationReason));
            revokeCertificateTasks.Add(revokeCertificateTask);

            var renewCertificateTask = Task.Run(async () =>
            {
                await connection.InvokeAsync("SendRenewCertificate");
            });

            renewCertificateTasks.Add(renewCertificateTask);
        }

        await Task.WhenAll(revokeCertificateTasks);
        await Task.WhenAll(renewCertificateTasks);
    }
}
