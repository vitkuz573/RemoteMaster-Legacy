// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using RemoteMaster.Host.Core.Abstractions;
using RemoteMaster.Host.Core.Extensions;
using RemoteMaster.Host.Linux.LinuxServices;
using RemoteMaster.Host.Linux.Services;
using Serilog;

namespace RemoteMaster.Host.Linux;

internal class Program
{
    private static async Task Main(string[] args)
    {
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.Console(
                outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
            .CreateLogger();

        var builder = WebApplication.CreateSlimBuilder(new WebApplicationOptions
        {
            ContentRootPath = AppContext.BaseDirectory
        });

        builder.Host.UseSerilog();

        builder.Configuration.AddCommandLine(args);

        string? commandName = null;

        if (args.Length > 0 && !args[0].StartsWith('-'))
        {
            commandName = args[0].ToLower();
        }

        ConfigureServices(builder.Services, commandName);

        var app = builder.Build();

        var rootCommand = app.Services.ConfigureCommands();

        var parseResult = rootCommand.Parse(args);
        var commandResult = parseResult.CommandResult;

        string? server = null;

        if (commandResult.Command.Name.Equals("install", StringComparison.OrdinalIgnoreCase))
        {
            var installCommand = rootCommand.Subcommands.First(c => c.Name == "install");
            var serverOption = installCommand.Options.First(o => o.Name == "--server");

            server = parseResult.GetValue<string>(serverOption.Name);
        }

        app.MapCoreHubs(commandName);

        var oneOffCommands = new HashSet<string> { "install", "update", "uninstall", "reinstall", "--help", "-h", "/?" };

        var shouldInvoke = args.Length == 0 ||
                           args[0].StartsWith('-') ||
                           (commandResult.Command != rootCommand && oneOffCommands.Contains(commandResult.Command.Name.ToLower()));

        var isUpdateCommand = commandName == "update";

        if (isUpdateCommand)
        {
            app.Lifetime.ApplicationStarted.Register(Callback);
        }
        else if (shouldInvoke)
        {
            var exitCode = await parseResult.InvokeAsync();

            Environment.Exit(exitCode);
        }

        await app.ConfigureSerilog(server);

        if (!app.Environment.IsDevelopment())
        {
            app.UseExceptionHandler("/Error");
        }

        if (commandName != "install")
        {
            app.UseAuthentication();
            app.UseAuthorization();
        }

        await app.RunAsync();

        return;

        async void Callback()
        {
            var exitCode = await parseResult.InvokeAsync();

            Environment.Exit(exitCode);
        }
    }

    private static void ConfigureServices(IServiceCollection services, string commandName)
    {
        services.AddHttpContextAccessor();

        services.AddTransient<INativeProcessFactory, NativeProcessFactory>();
        services.AddSingleton<IUserInstanceService, UserInstanceService>();
        services.AddSingleton<ICommandLineProvider, CommandLineProvider>();
        services.AddSingleton<IInputService, InputService>();
        services.AddSingleton<ITrayIconManager, TrayIconManager>();
        services.AddSingleton<IScriptService, ScriptService>();
        services.AddSingleton<IPowerService, PowerService>();
        services.AddSingleton<IService, HostService>();

        services.AddCoreServices(commandName);

        if (commandName != "install")
        {
            services.AddAuthorizationBuilder()
                .AddCoreRequirements()
                .AddCorePolicies();
        }
    }
}
