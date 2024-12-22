// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNetCore.SignalR.Client;
using RemoteMaster.Server.Aggregates.CrlAggregate.ValueObjects;

namespace RemoteMaster.Server.Components.Dialogs;

public partial class RenewCertificateDialog
{
    private readonly IEnumerable<X509RevocationReason> _revocationReasons = Enum.GetValues<X509RevocationReason>()
        .Except([X509RevocationReason.AACompromise, X509RevocationReason.RemoveFromCrl]);

    private X509RevocationReason _selectedRevocationReason = X509RevocationReason.Superseded;

    private async Task Confirm()
    {
        var revokeCertificateTasks = new List<Task>();
        var renewCertificateTasks = new List<Task>();

        foreach (var (_, connection) in Hosts)
        {
            connection!.On<string?>("ReceiveCertificateSerialNumber", serialNumberString =>
            {
                if (string.IsNullOrEmpty(serialNumberString))
                {
                    return;
                }

                var serialNumber = SerialNumber.FromExistingValue(serialNumberString);

                var revokeCertificateTask = Task.Run(async () =>
                {
                    await CrlService.RevokeCertificateAsync(serialNumber, _selectedRevocationReason);
                });

                revokeCertificateTasks.Add(revokeCertificateTask);
            });

            await connection!.InvokeAsync<string?>("GetCertificateSerialNumber");

            var renewCertificateTask = Task.Run(async () =>
            {
                await connection!.InvokeAsync("RenewCertificate");
            });

            renewCertificateTasks.Add(renewCertificateTask);
        }

        await Task.WhenAll(revokeCertificateTasks);
        await Task.WhenAll(renewCertificateTasks);
    }
}
