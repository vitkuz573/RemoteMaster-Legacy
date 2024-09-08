// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using Microsoft.AspNetCore.SignalR.Client;
using RemoteMaster.Server.Abstractions;
using RemoteMaster.Server.Aggregates.OrganizationAggregate;
using RemoteMaster.Server.Enums;
using Serilog;

namespace RemoteMaster.Server.Services;

public class CertificateRenewalTaskService(IServiceScopeFactory serviceScopeFactory) : IHostedService, IDisposable
{
    private Timer? _timer;

    public Task StartAsync(CancellationToken cancellationToken)
    {
        Log.Information("Starting certificate renewal task service.");
        
        _timer = new Timer(DoWork, null, TimeSpan.Zero, TimeSpan.FromMinutes(1));
        
        return Task.CompletedTask;
    }

    private async void DoWork(object? state)
    {
        using var scope = serviceScopeFactory.CreateScope();
        var organizationRepository = scope.ServiceProvider.GetRequiredService<IOrganizationRepository>();
        var accessTokenProvider = scope.ServiceProvider.GetRequiredService<IAccessTokenProvider>();
        var tokenService = scope.ServiceProvider.GetRequiredService<ITokenService>();

        var tasks = await organizationRepository.GetAllCertificateRenewalTasksAsync();

        var pendingTasks = tasks.Where(t => t.Status == CertificateRenewalStatus.Pending || t.Status == CertificateRenewalStatus.Failed).ToList();

        foreach (var task in pendingTasks)
        {
            try
            {
                Log.Information($"Processing task for computer: {task.Computer.Name}");

                await ProcessTaskAsync(task, tokenService);

                await organizationRepository.MarkCertificateRenewalTaskCompleted(task.Id);
                await organizationRepository.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"Error processing task for computer: {task.Computer.Name}");

                await organizationRepository.MarkCertificateRenewalTaskFailed(task.Id);
                await organizationRepository.SaveChangesAsync();
            }
        }
    }

    private static async Task ProcessTaskAsync(CertificateRenewalTask task, ITokenService tokenService)
    {
        await Task.Delay(1000);

        var hubConnection = new HubConnectionBuilder()
            .WithUrl($"https://{task.Computer.IpAddress}:5001/hubs/control", options =>
            {
                options.AccessTokenProvider = async () =>
                {
                    var accessTokenResult = await tokenService.GenerateTokensAsync("service_user");
                    var accessToken = accessTokenResult.Value.AccessToken;

                    return accessToken;
                };
            })
            .AddMessagePackProtocol()
            .Build();

        await hubConnection.StartAsync();

        await hubConnection.InvokeAsync("RenewCertificate");

        Log.Information($"Certificate renewal completed for computer: {task.Computer.Name}");
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        Log.Information("Stopping certificate renewal task service.");

        _timer?.Change(Timeout.Infinite, 0);

        return Task.CompletedTask;
    }

    public void Dispose()
    {
        _timer?.Dispose();
    }
}
