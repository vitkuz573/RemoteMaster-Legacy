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
    public static RootCommand ConfigureCommands(this IServiceProvider serviceProvider)
    {
        var rootCommand = new RootCommand("RemoteMaster Host");

        rootCommand.Subcommands.Add(CreateUserCommand());
        rootCommand.Subcommands.Add(CreateChatCommand());
        rootCommand.Subcommands.Add(CreateServiceCommand());
        rootCommand.Subcommands.Add(serviceProvider.CreateUpdateCommand());
        rootCommand.Subcommands.Add(serviceProvider.CreateUninstallCommand());
        rootCommand.Subcommands.Add(serviceProvider.CreateInstallCommand());
        rootCommand.Subcommands.Add(serviceProvider.CreateReinstallCommand());

        return rootCommand;
    }

    private static Command CreateUserCommand()
    {
        var command = new Command("user", "Runs the program in user mode.");

        return command;
    }

    private static Command CreateChatCommand()
    {
        var command = new Command("chat", "Runs the program in chat mode, enabling communication features.");

        return command;
    }

    private static Command CreateServiceCommand()
    {
        var command = new Command("service", "Runs the program as a service.");

        return command;
    }

    private static Command CreateUpdateCommand(this IServiceProvider serviceProvider)
    {
        var command = new Command("update", "Updates the program to the latest version.");

        var folderPathOption = new Option<string>("--folder-path", "--fp")
        {
            Description = "Specifies the folder path for the update operation.",
            Required = true
        };

        var usernameOption = new Option<string>("--username", "--user", "-u")
        {
            Description = "Specifies the username for authentication."
        };

        var passwordOption = new Option<string>("--password", "-pass", "-p")
        {
            Description = "Specifies the password for authentication."
        };

        var forceOption = new Option<bool>("--force", "-f")
        {
            Description = "Forces the update operation to proceed, even if no update is needed."
        };

        var allowDowngradeOption = new Option<bool>("--allow-downgrade", "--ad")
        {
            Description = "Allows the update operation to proceed with a lower version than the current one."
        };

        var waitForClientConnectionTimeoutOption = new Option<int>("--wait-for-client-connection-timeout", "--wait-connection-timeout", "-w")
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

    private static Command CreateUninstallCommand(this IServiceProvider serviceProvider)
    {
        var command = new Command("uninstall", "Removes the program and its components.");

        var maxAttemptsOption = new Option<int>("--max-attempts", "--ma")
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

        var initialRetryDelayOption = new Option<int>("--initial-retry-delay", "--ir")
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

        var maxRetryDelayOption = new Option<int>("--max-retry-delay", "--mr")
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

    private static Command CreateInstallCommand(this IServiceProvider serviceProvider)
    {
        var command = new Command("install", "Installs the necessary components for the program.");

        var serverOption = new Option<string>("--server", "--srv")
        {
            Description = "Specifies the server where the host will be registered.",
            Required = true
        };

        var organizationOption = new Option<string>("--organization", "--org")
        {
            Description = "Specifies the name of the organization where the host is registered.",
            Required = true
        };

        var organizationalUnitOption = new Option<List<string>>("--organizational-unit", "--ou")
        {
            Description = "Specifies the organizational unit where the host is registered.",
            AllowMultipleArgumentsPerToken = true,
            Required = true
        };

        var forceOption = new Option<bool>("--force", "-f")
        {
            Description = "Forcibly register the host in the specified organizational unit, overriding any existing registrations.",
            Required = false
        };

        var maxAttemptsOption = new Option<int>("--max-attempts", "--ma")
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

        var initialRetryDelayOption = new Option<int>("--initial-retry-delay", "--ir")
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

        var maxRetryDelayOption = new Option<int>("--max-retry-delay", "--mr")
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
        command.Options.Add(forceOption);
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
            var force = parseResult.GetValue(forceOption);

            var logger = serviceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("Install");
            var serverAvailabilityService = serviceProvider.GetRequiredService<IServerAvailabilityService>();
            var hostInstaller = serviceProvider.GetRequiredService<IHostInstaller>();

            if (!await serverAvailabilityService.IsServerAvailableAsync(server, maxAttempts, initialRetryDelay, maxRetryDelay, cancellationToken))
            {
                logger.LogError("The server {Server} is unavailable. Installation will not proceed.", server);

                return 1;
            }

            var installRequest = new HostInstallRequest(server, organization, organizationalUnit, force);

            await hostInstaller.InstallAsync(installRequest);

            return 0;
        });

        return command;
    }

    private static Command CreateReinstallCommand(this IServiceProvider serviceProvider)
    {
        var command = new Command("reinstall", "Reinstalls the program using the current configuration or specified parameters.");

        var serverOption = new Option<string>("--server", "--srv")
        {
            Description = "Overrides the current configuration server."
        };

        var organizationOption = new Option<string>("--organization", "--org")
        {
            Description = "Overrides the current configuration organization."
        };

        var organizationalUnitOption = new Option<List<string>>("--organizational-unit", "--ou")
        {
            Description = "Overrides the current configuration organizational unit.",
            AllowMultipleArgumentsPerToken = true
        };

        var maxAttemptsOption = new Option<int>("--max-attempts", "--ma")
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

        var initialRetryDelayOption = new Option<int>("--initial-retry-delay", "--ir")
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

        var maxRetryDelayOption = new Option<int>("--max-retry-delay", "--mr")
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

            if (organizationalUnit.Count == 0)
            {
                organizationalUnit = currentConfig.Subject.OrganizationalUnit;
            }

            if (string.IsNullOrWhiteSpace(server) || string.IsNullOrWhiteSpace(organization) || organizationalUnit.Count == 0)
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

            var installRequest = new HostInstallRequest(server, organization, organizationalUnit, false);

            await hostInstaller.InstallAsync(installRequest);

            return 0;
        });

        return command;
    }
}
