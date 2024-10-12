// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Text.Json;
using Microsoft.Extensions.Logging;
using RemoteMaster.Host.Core.Abstractions;
using RemoteMaster.Host.Core.JsonContexts;
using RemoteMaster.Host.Core.Models;
using RemoteMaster.Host.Windows.Models;

namespace RemoteMaster.Host.Windows.Services;

public class ModuleService : IModuleService
{
    private readonly Dictionary<string, IModule> _modules = [];

    private readonly ILogger<ModuleService> _logger;
    public ModuleService(ILogger<ModuleService> logger)
    {
        _logger = logger;

        LoadModules();
    }

    private void LoadModules()
    {
        var programFilesPath = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
        var modulesDirectory = Path.Combine(programFilesPath, "RemoteMaster", "Host", "Modules");

        if (!Directory.Exists(modulesDirectory))
        {
            Console.WriteLine($"Error: Modules directory not found: {modulesDirectory}");

            return;
        }

        var directories = Directory.GetDirectories(modulesDirectory);

        foreach (var directory in directories)
        {
            var moduleInfoPath = Path.Combine(directory, "module-info.json");

            if (!File.Exists(moduleInfoPath))
            {
                _logger.LogWarning("No module-info.json in {ModuleDirectory}", directory);

                continue;
            }

            try
            {
                var json = File.ReadAllText(moduleInfoPath);
                var moduleInfo = JsonSerializer.Deserialize(json, ModuleInfoJsonSerializerContext.Default.ModuleInfo);

                if (moduleInfo != null)
                {
                    var module = CreateModule(moduleInfo, directory);

                    _modules.Add(moduleInfo.Name, module);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Error loading module from {ModuleDirectory}: {Message}", directory, ex.Message);
            }
        }
    }

    public IModule? GetModule(string name)
    {
        _modules.TryGetValue(name, out var module);

        return module;
    }

    private static IModule CreateModule(ModuleInfo moduleInfo, string moduleDirectory)
    {
        return new Module(moduleInfo, moduleDirectory);
    }
}
