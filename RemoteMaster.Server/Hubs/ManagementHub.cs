// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using Microsoft.AspNetCore.SignalR;
using Org.BouncyCastle.Pkcs;
using RemoteMaster.Server.Abstractions;
using RemoteMaster.Server.Models;
using RemoteMaster.Server.Services;

namespace RemoteMaster.Server.Hubs;

public class ManagementHub : Hub
{
    private readonly ICertificateService _certificateService;
    private readonly DatabaseService _databaseService;
    private readonly ILogger<ManagementHub> _logger;

    public ManagementHub(ICertificateService certificateService, DatabaseService databaseService, ILogger<ManagementHub> logger)
    {
        _certificateService = certificateService;
        _databaseService = databaseService;
        _logger = logger;
    }

    public async Task<bool> RegisterHostAsync(string hostName, string ipAddress, string macAddress, string group, byte[] csrBytes)
    {
        Pkcs10CertificationRequest csr;

        try
        {
            csr = new Pkcs10CertificationRequest(csrBytes);
        }
        catch (Exception ex)
        {
            _logger.LogError("Failed to parse CSR: {Message}", ex.Message);

            return false;
        }

        var certificate = _certificateService.GenerateCertificateFromCSR(csr);

        await Clients.Caller.SendAsync("ReceiveCertificate", certificate.GetEncoded());

        var folder = (await _databaseService.GetNodesAsync(f => f.Name == group && f is Folder)).OfType<Folder>().FirstOrDefault();

        if (folder == null)
        {
            folder = new Folder(group);
            await _databaseService.AddNodeAsync(folder);
        }

        var existingComputer = (await _databaseService.GetChildrenByParentIdAsync<Computer>(folder.NodeId)).FirstOrDefault(c => c.Name == hostName);

        if (existingComputer != null)
        {
            existingComputer.IPAddress = ipAddress;
        }
        else
        {
            var computer = new Computer
            {
                Name = hostName,
                IPAddress = ipAddress,
                MACAddress = macAddress,
                Parent = folder
            };

            await _databaseService.AddNodeAsync(computer);
        }

        return true;
    }

    public async Task<bool> UnregisterHostAsync(string hostName, string group)
    {
        var folder = (await _databaseService.GetNodesAsync(f => f.Name == group && f is Folder)).OfType<Folder>().FirstOrDefault();

        if (folder == null)
        {
            _logger.LogWarning("Unregistration failed: Folder '{Group}' not found.", group);

            return false;
        }

        var existingComputer = (await _databaseService.GetChildrenByParentIdAsync<Computer>(folder.NodeId))
                               .FirstOrDefault(c => c.Name == hostName);

        if (existingComputer != null)
        {
            await _databaseService.RemoveNodeAsync(existingComputer);

            var remainingComputers = await _databaseService.GetChildrenByParentIdAsync<Computer>(folder.NodeId);

            if (!remainingComputers.Any())
            {
                await _databaseService.RemoveNodeAsync(folder);
            }

            return true;
        }

        _logger.LogWarning("Unregistration failed: Computer '{HostName}' not found in folder '{Group}'.", hostName, group);

        return false;
    }
}