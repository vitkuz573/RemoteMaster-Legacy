// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.CommandLine;
using System.CommandLine.Parsing;
using System.Text.Json;
using RemoteMaster.Client.Abstractions;
using RemoteMaster.Client.Core.Abstractions;
using RemoteMaster.Client.Core.Extensions;
using RemoteMaster.Client.Services;
using RemoteMaster.Shared.Models;

var serviceProvider = BuildServiceProvider();
var installationService = serviceProvider.GetRequiredService<IInstallationService>();

var rootCommand = new RootCommand();

var installOption = new Option<bool>("--install", "Install the application");
rootCommand.AddOption(installOption);

rootCommand.SetHandler(async () =>
{
    var configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.json");

    if (File.Exists(configPath))
    {
        if (rootCommand.Parse(args).HasOption(installOption))
        {
            DisplayCoolHeader();

            var configContent = await File.ReadAllTextAsync(configPath);
            var configData = JsonSerializer.Deserialize<ConfigurationModel>(configContent);

            DisplayConfig(configData);
            await AskForInstallation(installationService, configData);
        }
        else
        {
            var host = CreateHostBuilder(args).Build();
            host.Run();
        }
    }
    else
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine("Config file is missing.");
        Console.ResetColor();
        Environment.Exit(1);
    }
});

rootCommand.Invoke(args);

ServiceProvider BuildServiceProvider() =>
    new ServiceCollection()
        .AddSingleton<IInstallationService, InstallationService>()
        .BuildServiceProvider();

IHostBuilder CreateHostBuilder(string[] args) =>
    Host.CreateDefaultBuilder(args)
        .ConfigureServices((hostContext, services) =>
        {
            services.AddCoreServices();
            services.AddSingleton<IScreenCapturerService, BitBltCapturer>();
            services.AddSingleton<ICursorRenderService, CursorRenderService>();
            services.AddSingleton<IInputService, InputService>();
            services.AddSingleton<IPowerService, PowerService>();
        });

void DisplayCoolHeader()
{
    Console.ForegroundColor = ConsoleColor.Green;
    Console.WriteLine("#################################");
    Console.WriteLine("##      RemoteMaster Client    ##");
    Console.WriteLine("#################################");
    Console.ResetColor();
}

void DisplayConfig(ConfigurationModel configData)
{
    Console.ForegroundColor = ConsoleColor.Yellow;
    Console.WriteLine("Configuration:");
    Console.ResetColor();
    Console.WriteLine($"Server: {configData.Server}");
    Console.WriteLine($"Group: {configData.Group}");
}

async Task AskForInstallation(IInstallationService installationService, ConfigurationModel configData)
{
    Console.ForegroundColor = ConsoleColor.Blue;
    Console.Write("\nDo you want to install? [Y/N]: ");
    Console.ResetColor();

    var key = Console.ReadKey().Key;
    Console.WriteLine();

    if (key == ConsoleKey.Y)
    {
        var success = await installationService.InstallClientAsync(configData);
        Console.WriteLine(success ? "Installation succeeded." : "Installation failed.");
    }
    else if (key == ConsoleKey.N)
    {
        Console.WriteLine("Installation cancelled.");
    }
    else
    {
        Console.WriteLine("Invalid input. Installation cancelled.");
    }
}
