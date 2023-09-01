using System.CommandLine;
using System.CommandLine.Parsing;
using System.Text.Json;
using RemoteMaster.Client.Abstractions;
using RemoteMaster.Client.Core.Abstractions;
using RemoteMaster.Client.Core.Extensions;
using RemoteMaster.Client.Services;
using RemoteMaster.Shared.Models;

var rootCommand = new RootCommand();

var installOption = new Option<bool>("--install", "Install the application");
rootCommand.AddOption(installOption);

rootCommand.SetHandler(async () =>
{
    var configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.json");
    DisplayCoolHeader();

    if (File.Exists(configPath))
    {
        if (rootCommand.Parse(args).HasOption(installOption))
        {
            var configContent = await File.ReadAllTextAsync(configPath);
            var configData = JsonSerializer.Deserialize<ConfigurationModel>(configContent);

            DisplayConfig(configData);
            AskForInstallation();
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

void AskForInstallation()
{
    Console.ForegroundColor = ConsoleColor.Blue;
    Console.Write("\nDo you want to install? [Y/N]: ");
    Console.ResetColor();

    var key = Console.ReadKey().Key;
    Console.WriteLine();

    if (key == ConsoleKey.Y)
    {
        Console.WriteLine("Installing...");
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

