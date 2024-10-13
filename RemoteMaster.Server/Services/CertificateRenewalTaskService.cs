// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using MessagePack.Resolvers;
using MessagePack;
using Microsoft.AspNetCore.SignalR.Client;
using RemoteMaster.Server.Abstractions;
using RemoteMaster.Server.Aggregates.OrganizationAggregate;
using RemoteMaster.Server.Enums;
using RemoteMaster.Shared.Formatters;

namespace RemoteMaster.Server.Services;

public class CertificateRenewalTaskService(IServiceScopeFactory serviceScopeFactory, ILogger<CertificateRenewalTaskService> logger) : IHostedService, IDisposable
{
    private Timer? _timer;

    public Task StartAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Starting certificate renewal task service.");
        
        _timer = new Timer(DoWork, null, TimeSpan.Zero, TimeSpan.FromMinutes(1));
        
        return Task.CompletedTask;
    }

    private async void DoWork(object? state)
    {
        using var scope = serviceScopeFactory.CreateScope();
        var applicationUnitOfWork = scope.ServiceProvider.GetRequiredService<IApplicationUnitOfWork>();
        var accessTokenProvider = scope.ServiceProvider.GetRequiredService<IAccessTokenProvider>();
        var tokenService = scope.ServiceProvider.GetRequiredService<ITokenService>();

        var tasks = await applicationUnitOfWork.Organizations.GetAllCertificateRenewalTasksAsync();

        var pendingTasks = tasks.Where(t => t.Status == CertificateRenewalStatus.Pending || t.Status == CertificateRenewalStatus.Failed).ToList();

        foreach (var task in pendingTasks)
        {
            try
            {
                logger.LogInformation("Processing task for host: {HostName}", task.Host!.Name);

                await ProcessTaskAsync(task, tokenService);

                await applicationUnitOfWork.Organizations.MarkCertificateRenewalTaskCompleted(task.Id);
                await applicationUnitOfWork.CommitAsync();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error processing task for host: {HostName}", task.Host!.Name);

                await applicationUnitOfWork.Organizations.MarkCertificateRenewalTaskFailed(task.Id);
                await applicationUnitOfWork.CommitAsync();
            }
        }
    }

    private async Task ProcessTaskAsync(CertificateRenewalTask task, ITokenService tokenService)
    {
        await Task.Delay(1000);

        var hubConnection = new HubConnectionBuilder()
            .WithUrl($"https://{task.Host!.IpAddress}:5001/hubs/control", options =>
            {
                options.AccessTokenProvider = async () =>
                {
                    var accessTokenResult = await tokenService.GenerateTokensAsync("service_user");
                    var accessToken = accessTokenResult.Value.AccessToken;

                    return accessToken;
                };
            })
            .AddMessagePackProtocol(options =>
            {
                var resolver = CompositeResolver.Create([new IPAddressFormatter(), new PhysicalAddressFormatter()], [ContractlessStandardResolver.Instance]);

                options.SerializerOptions = MessagePackSerializerOptions.Standard.WithResolver(resolver);
            })
            .Build();

        await hubConnection.StartAsync();

        await hubConnection.InvokeAsync("RenewCertificate");

        logger.LogInformation("Certificate renewal completed for host: {HostName}", task.Host.Name);
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
