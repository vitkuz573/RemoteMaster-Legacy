// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using Microsoft.AspNetCore.SignalR.Client;

namespace RemoteMaster.Server.Components.Dialogs;

public partial class RenewCertificateDialog
{
    private async Task Confirm()
    {
        var renewCertificateTasks = new List<Task>();

        foreach (var (computer, connection) in Hosts)
        {
            var renewCertificateTask = Task.Run(async () =>
            {
                await connection.InvokeAsync("SendRenewCertificate");
            });

            renewCertificateTasks.Add(renewCertificateTask);
        }

        await Task.WhenAll(renewCertificateTasks);
    }
}
