// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.CommandLine;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RemoteMaster.Host.Core.Abstractions;
using RemoteMaster.Host.Core.Models;

namespace RemoteMaster.Host.Core.Extensions;

public static class ServiceProviderExtensions
{
    public static CliRootCommand ConfigureCommands(this IServiceProvider serviceProvider)
    {
        var rootCommand = new CliRootCommand("RemoteMaster Host");

        rootCommand.Subcommands.Add(CreateUserCommand());
        rootCommand.Subcommands.Add(CreateChatCommand());
        rootCommand.Subcommands.Add(CreateServiceCommand());
        rootCommand.Subcommands.Add(serviceProvider.CreateUpdateCommand());
        rootCommand.Subcommands.Add(serviceProvider.CreateUninstallCommand());
        rootCommand.Subcommands.Add(serviceProvider.CreateInstallCommand());
        rootCommand.Subcommands.Add(serviceProvider.CreateReinstallCommand());

        return rootCommand;
    }

    private static CliCommand CreateUserCommand()
    {
        var command = new CliCommand("user", "Runs the program in user mode.");

        return command;
    }

    private static CliCommand CreateChatCommand()
    {
        var command = new CliCommand("chat", "Runs the program in chat mode, enabling communication features.");

        return command;
    }

    private static CliCommand CreateServiceCommand()
    {
        var command = new CliCommand("service", "Runs the program as a service.");

        return command;
    }

    private static CliCommand CreateUpdateCommand(this IServiceProvider serviceProvider)
    {
        var command = new CliCommand("update", "Updates the program to the latest version.");

        var folderPathOption = new CliOption<string>("--folder-path", "--fp")
        {
            Description = "Specifies the folder path for the update operation.",
            Required = true
        };

        var usernameOption = new CliOption<string>("--username", "--user", "-u")
        {
            Description = "Specifies the username for authentication."
        };

        var passwordOption = new CliOption<string>("--password", "-pass", "-p")
        {
            Description = "Specifies the password for authentication."
        };

        var forceOption = new CliOption<bool>("--force", "-f")
        {
            Description = "Forces the update operation to proceed, even if no update is needed."
        };

        var allowDowngradeOption = new CliOption<bool>("--allow-downgrade", "--ad")
        {
            Description = "Allows the update operation to proceed with a lower version than the current one."
        };

        var waitForClientConnectionTimeoutOption = new CliOption<int>("--wait-for-client-connection-timeout", "--wait-connection-timeout", "-w")
        {
            Description = "Specifies the maximum time to wait for a client to connect before proceeding with the update, in milliseconds.",
            DefaultValueFactory = _ => 0
        };

        waitForClientConnectionTimeoutOption.Validators.Add(result =>
        {
            var value = result.GetValueOrDefault<int>();

            if (value < 0)
            {
                result.AddError("The --wait-for-client-connection-timeout option must be a positive integer.");
            }
        });

        command.Options.Add(folderPathOption);
        command.Options.Add(usernameOption);
        command.Options.Add(passwordOption);
        command.Options.Add(forceOption);
        command.Options.Add(allowDowngradeOption);
        command.Options.Add(waitForClientConnectionTimeoutOption);

        command.SetAction(async (parseResult, _) =>
        {
            var folderPath = parseResult.GetValue(folderPathOption);
            var username = parseResult.GetValue(usernameOption);
            var password = parseResult.GetValue(passwordOption);
            var force = parseResult.GetValue(forceOption);
            var allowDowngrade = parseResult.GetValue(allowDowngradeOption);
            var waitForClientConnectionTimeout = parseResult.GetValue(waitForClientConnectionTimeoutOption);

            var hostUpdater = serviceProvider.GetRequiredService<IHostUpdater>();

            await hostUpdater.UpdateAsync(folderPath, username, password, force, allowDowngrade, waitForClientConnectionTimeout);
            
            return 0;
        });

        return command;
    }

    private static CliCommand CreateUninstallCommand(this IServiceProvider serviceProvider)
    {
        var command = new CliCommand("uninstall", "Removes the program and its components.");

        var maxAttemptsOption = new CliOption<int>("--max-attempts", "--ma")
        {
            Description = "Specifies the maximum number of connection attempts to check server availability.",
            Required = false,
            DefaultValueFactory = _ => 5
        };

        maxAttemptsOption.Validators.Add(result =>
        {
            var value = result.GetValueOrDefault<int>();

            if (value <= 0)
            {
                result.AddError("The --max-attempts option must be a positive integer.");
            }
        });

        var initialRetryDelayOption = new CliOption<int>("--initial-retry-delay", "--ir")
        {
            Description = "Specifies the initial delay before retrying a connection attempt (in milliseconds).",
            Required = false,
            DefaultValueFactory = _ => 1000
        };

        initialRetryDelayOption.Validators.Add(result =>
        {
            var value = result.GetValueOrDefault<int>();

            if (value < 0)
            {
                result.AddError("The --initial-retry-delay option must be a non-negative integer.");
            }
        });

        var maxRetryDelayOption = new CliOption<int>("--max-retry-delay", "--mr")
        {
            Description = "Specifies the maximum delay between connection attempts (in milliseconds).",
            Required = false,
            DefaultValueFactory = _ => 5000
        };

        maxRetryDelayOption.Validators.Add(result =>
        {
            var value = result.GetValueOrDefault<int>();

            if (value < 0)
            {
                result.AddError("The --max-retry-delay option must be a non-negative integer.");
            }
        });

        command.Options.Add(maxAttemptsOption);
        command.Options.Add(initialRetryDelayOption);
        command.Options.Add(maxRetryDelayOption);

        command.SetAction(async (parseResult, cancellationToken) =>
        {
            var maxAttempts = parseResult.GetValue(maxAttemptsOption);
            var initialRetryDelay = parseResult.GetValue(initialRetryDelayOption);
            var maxRetryDelay = parseResult.GetValue(maxRetryDelayOption);

            var logger = serviceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("Uninstall");
            var serverAvailabilityService = serviceProvider.GetRequiredService<IServerAvailabilityService>();
            var hostConfigurationService = serviceProvider.GetRequiredService<IHostConfigurationService>();
            var hostUninstaller = serviceProvider.GetRequiredService<IHostUninstaller>();

            var currentConfig = await hostConfigurationService.LoadAsync();

            var server = currentConfig.Server;

            if (!await serverAvailabilityService.IsServerAvailableAsync(server, maxAttempts, initialRetryDelay, maxRetryDelay, cancellationToken))
            {
                logger.LogError("The server {Server} is unavailable. Uninstallation will not proceed.", server);

                return 1;
            }

            await hostUninstaller.UninstallAsync();

            return 0;
        });

        return command;
    }

    private static CliCommand CreateInstallCommand(this IServiceProvider serviceProvider)
    {
        var command = new CliCommand("install", "Installs the necessary components for the program.");

        var serverOption = new CliOption<string>("--server", "--srv")
        {
            Description = "Specifies the server where the host will be registered.",
            Required = true
        };

        var organizationOption = new CliOption<string>("--organization", "--org")
        {
            Description = "Specifies the name of the organization where the host is registered.",
            Required = true
        };

        var organizationalUnitOption = new CliOption<string>("--organizational-unit", "--ou")
        {
            Description = "Specifies the organizational unit where the host is registered.",
            Required = true
        };

        var maxAttemptsOption = new CliOption<int>("--max-attempts", "--ma")
        {
            Description = "Specifies the maximum number of connection attempts to check server availability.",
            Required = false,
            DefaultValueFactory = _ => 5
        };

        maxAttemptsOption.Validators.Add(result =>
        {
            var value = result.GetValueOrDefault<int>();

            if (value <= 0)
            {
                result.AddError("The --max-attempts option must be a positive integer.");
            }
        });

        var initialRetryDelayOption = new CliOption<int>("--initial-retry-delay", "--ir")
        {
            Description = "Specifies the initial delay before retrying a connection attempt (in milliseconds).",
            Required = false,
            DefaultValueFactory = _ => 1000
        };

        initialRetryDelayOption.Validators.Add(result =>
        {
            var value = result.GetValueOrDefault<int>();

            if (value < 0)
            {
                result.AddError("The --initial-retry-delay option must be a non-negative integer.");
            }
        });

        var maxRetryDelayOption = new CliOption<int>("--max-retry-delay", "--mr")
        {
            Description = "Specifies the maximum delay between connection attempts (in milliseconds).",
            Required = false,
            DefaultValueFactory = _ => 5000
        };

        maxRetryDelayOption.Validators.Add(result =>
        {
            var value = result.GetValueOrDefault<int>();

            if (value < 0)
            {
                result.AddError("The --max-retry-delay option must be a non-negative integer.");
            }
        });

        command.Options.Add(serverOption);
        command.Options.Add(organizationOption);
        command.Options.Add(organizationalUnitOption);
        command.Options.Add(maxAttemptsOption);
        command.Options.Add(initialRetryDelayOption);
        command.Options.Add(maxRetryDelayOption);

        command.SetAction(async (parseResult, cancellationToken) =>
        {
            var maxAttempts = parseResult.GetValue(maxAttemptsOption);
            var initialRetryDelay = parseResult.GetValue(initialRetryDelayOption);
            var maxRetryDelay = parseResult.GetValue(maxRetryDelayOption);

            var server = parseResult.GetValue(serverOption);
            var organization = parseResult.GetValue(organizationOption);
            var organizationalUnit = parseResult.GetValue(organizationalUnitOption);

            var logger = serviceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("Install");
            var serverAvailabilityService = serviceProvider.GetRequiredService<IServerAvailabilityService>();
            var hostInstaller = serviceProvider.GetRequiredService<IHostInstaller>();

            if (!await serverAvailabilityService.IsServerAvailableAsync(server, maxAttempts, initialRetryDelay, maxRetryDelay, cancellationToken))
            {
                logger.LogError("The server {Server} is unavailable. Installation will not proceed.", server);

                return 1;
            }

            var installRequest = new HostInstallRequest(server, organization, organizationalUnit);

            await hostInstaller.InstallAsync(installRequest);

            return 0;
        });

        return command;
    }

    private static CliCommand CreateReinstallCommand(this IServiceProvider serviceProvider)
    {
        var command = new CliCommand("reinstall", "Reinstalls the program using the current configuration or specified parameters.");

        var serverOption = new CliOption<string>("--server", "--srv")
        {
            Description = "Overrides the current configuration server."
        };

        var organizationOption = new CliOption<string>("--organization", "--org")
        {
            Description = "Overrides the current configuration organization."
        };

        var organizationalUnitOption = new CliOption<string>("--organizational-unit", "--ou")
        {
            Description = "Overrides the current configuration organizational unit."
        };

        var maxAttemptsOption = new CliOption<int>("--max-attempts", "--ma")
        {
            Description = "Specifies the maximum number of connection attempts to check server availability.",
            Required = false,
            DefaultValueFactory = _ => 5
        };

        maxAttemptsOption.Validators.Add(result =>
        {
            var value = result.GetValueOrDefault<int>();

            if (value <= 0)
            {
                result.AddError("The --max-attempts option must be a positive integer.");
            }
        });

        var initialRetryDelayOption = new CliOption<int>("--initial-retry-delay", "--ir")
        {
            Description = "Specifies the initial delay before retrying a connection attempt (in milliseconds).",
            Required = false,
            DefaultValueFactory = _ => 1000
        };

        initialRetryDelayOption.Validators.Add(result =>
        {
            var value = result.GetValueOrDefault<int>();

            if (value < 0)
            {
                result.AddError("The --initial-retry-delay option must be a non-negative integer.");
            }
        });

        var maxRetryDelayOption = new CliOption<int>("--max-retry-delay", "--mr")
        {
            Description = "Specifies the maximum delay between connection attempts (in milliseconds).",
            Required = false,
            DefaultValueFactory = _ => 5000
        };

        maxRetryDelayOption.Validators.Add(result =>
        {
            var value = result.GetValueOrDefault<int>();

            if (value < 0)
            {
                result.AddError("The --max-retry-delay option must be a non-negative integer.");
            }
        });

        command.Options.Add(serverOption);
        command.Options.Add(organizationOption);
        command.Options.Add(organizationalUnitOption);
        command.Options.Add(maxAttemptsOption);
        command.Options.Add(initialRetryDelayOption);
        command.Options.Add(maxRetryDelayOption);

        command.SetAction(async (parseResult, cancellationToken) =>
        {
            var server = parseResult.GetValue(serverOption);
            var organization = parseResult.GetValue(organizationOption);
            var organizationalUnit = parseResult.GetValue(organizationalUnitOption);
            var maxAttempts = parseResult.GetValue(maxAttemptsOption);
            var initialRetryDelay = parseResult.GetValue(initialRetryDelayOption);
            var maxRetryDelay = parseResult.GetValue(maxRetryDelayOption);

            var logger = serviceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("Reinstall");
            var serverAvailabilityService = serviceProvider.GetRequiredService<IServerAvailabilityService>();
            var hostConfigurationService = serviceProvider.GetRequiredService<IHostConfigurationService>();
            var hostUninstaller = serviceProvider.GetRequiredService<IHostUninstaller>();
            var hostInstaller = serviceProvider.GetRequiredService<IHostInstaller>();

            var currentConfig = await hostConfigurationService.LoadAsync();

            server ??= currentConfig.Server;
            organization ??= currentConfig.Subject.Organization;
            organizationalUnit ??= currentConfig.Subject.OrganizationalUnit.FirstOrDefault();

            if (string.IsNullOrWhiteSpace(server) || string.IsNullOrWhiteSpace(organization) || string.IsNullOrWhiteSpace(organizationalUnit))
            {
                logger.LogError("The configuration is incomplete or invalid.");

                return 1;
            }

            if (!await serverAvailabilityService.IsServerAvailableAsync(server, maxAttempts, initialRetryDelay, maxRetryDelay, cancellationToken))
            {
                logger.LogError("The server {Server} is unavailable. Reinstallation will not proceed.", server);

                return 1;
            }

            await hostUninstaller.UninstallAsync();

            var installRequest = new HostInstallRequest(server, organization, organizationalUnit);

            await hostInstaller.InstallAsync(installRequest);

            return 0;
        });

        return command;
    }
}
