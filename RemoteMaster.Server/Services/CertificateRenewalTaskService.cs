// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using Microsoft.AspNetCore.SignalR.Client;
using RemoteMaster.Server.Abstractions;
using RemoteMaster.Server.Aggregates.CertificateRenewalTaskAggregate;
using RemoteMaster.Server.Enums;
using RemoteMaster.Shared.Extensions;

namespace RemoteMaster.Server.Services;

public class CertificateRenewalTaskService(IServiceScopeFactory serviceScopeFactory, ILogger<CertificateRenewalTaskService> logger) : IHostedService, IDisposable
{
    private Timer? _timer;

    public Task StartAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Starting certificate renewal task service.");
        
        _timer = new Timer(DoWorkAsync, null, TimeSpan.Zero, TimeSpan.FromMinutes(1));
        
        return Task.CompletedTask;
    }

    private async void DoWorkAsync(object? state)
    {
        using var scope = serviceScopeFactory.CreateScope();
        var applicationUnitOfWork = scope.ServiceProvider.GetRequiredService<IApplicationUnitOfWork>();
        var certificateTaskUnitOfWork = scope.ServiceProvider.GetRequiredService<ICertificateTaskUnitOfWork>();
        var tokenService = scope.ServiceProvider.GetRequiredService<ITokenService>();

        var tasks = await certificateTaskUnitOfWork.CertificateRenewalTasks.GetAllAsync();

        var pendingTasks = tasks
            .Where(t => t.Status is CertificateRenewalStatus.Pending or CertificateRenewalStatus.Failed)
            //.Where(t => t.RenewalSchedule.PlannedDate <= DateTimeOffset.Now)
            .ToList();

        foreach (var task in pendingTasks)
        {
            var host = (await applicationUnitOfWork.Organizations.FindHostsAsync(h => h.Id == task.HostId)).FirstOrDefault();

            CertificateRenewalStatus newStatus;

            try
            {
                logger.LogInformation("Processing task for host: {HostName}", host!.Name);

                await ProcessTaskAsync(applicationUnitOfWork, task, tokenService);
                newStatus = CertificateRenewalStatus.Completed;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error processing task for host: {HostName}", host!.Name);

                newStatus = CertificateRenewalStatus.Failed;
            }

            task.SetStatus(newStatus);
            task.RenewalSchedule.SetLastAttemptDate(DateTimeOffset.Now);

            await certificateTaskUnitOfWork.CommitAsync();
        }
    }

    private async Task ProcessTaskAsync(IApplicationUnitOfWork applicationUnitOfWork, CertificateRenewalTask task, ITokenService tokenService)
    {
        await Task.Delay(1000);

        var host = (await applicationUnitOfWork.Organizations.FindHostsAsync(h => h.Id == task.HostId)).FirstOrDefault();

        var hubConnection = new HubConnectionBuilder()
            .WithUrl($"https://{host!.IpAddress}:5001/hubs/certificate", options =>
            {
                options.AccessTokenProvider = async () =>
                {
                    var accessTokenResult = await tokenService.GenerateTokensAsync("service_user");
                    var accessToken = accessTokenResult.Value.AccessToken;

                    return accessToken;
                };
            })
            .AddMessagePackProtocol(options => options.Configure())
            .Build();

        await hubConnection.StartAsync();

        await hubConnection.InvokeAsync("RenewCertificate");

        logger.LogInformation("Certificate renewal completed for host: {HostName}", host.Name);
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Stopping certificate renewal task service.");

        _timer?.Change(Timeout.Infinite, 0);

        return Task.CompletedTask;
    }

    public void Dispose()
    {
        _timer?.Dispose();
    }
}
