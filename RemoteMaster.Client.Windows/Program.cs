// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Text.Json;
using RemoteMaster.Client.Abstractions;
using RemoteMaster.Client.Core.Abstractions;
using RemoteMaster.Client.Core.Extensions;
using RemoteMaster.Client.Services;
using RemoteMaster.Shared.Models;

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

void AskForInstallation()
{
    Console.ForegroundColor = ConsoleColor.Blue;
    Console.WriteLine("\nDo you want to install?");
    Console.ResetColor();
    Console.ReadLine();
}

var configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.json");

DisplayCoolHeader();

if (File.Exists(configPath))
{
    var configContent = File.ReadAllText(configPath);
    var configData = JsonSerializer.Deserialize<ConfigurationModel>(configContent);

    DisplayConfig(configData);

    if (args.Length > 0 && args[0] == "install")
    {
        AskForInstallation();
        Environment.Exit(0);
    }

    var builder = WebApplication.CreateBuilder(args).ConfigureCoreUrls();

    builder.Services.AddCoreServices();
    builder.Services.AddSingleton<IScreenCapturerService, BitBltCapturer>();
    builder.Services.AddSingleton<ICursorRenderService, CursorRenderService>();
    builder.Services.AddSingleton<IInputService, InputService>();
    builder.Services.AddSingleton<IPowerService, PowerService>();

    var app = builder.Build();

    app.MapCoreHubs();

    app.Run();
}
else
{
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine("Config file is missing.");
    Console.ResetColor();
    Environment.Exit(1);
}
