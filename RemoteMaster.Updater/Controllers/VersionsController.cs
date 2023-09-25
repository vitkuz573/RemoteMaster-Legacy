// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using Microsoft.AspNetCore.Mvc;
using RemoteMaster.Updater.Abstractions;
using RemoteMaster.Updater.Models;

namespace RemoteMaster.Updater.Controllers;

[Route("api/[controller]")]
[ApiController]
public class VersionsController : ControllerBase
{
    private readonly IEnumerable<IComponentUpdater> _componentUpdaters;

    public VersionsController(IEnumerable<IComponentUpdater> componentUpdaters)
    {
        _componentUpdaters = componentUpdaters;
    }

    [HttpGet]
    public async Task<IActionResult> GetVersions()
    {
        var versions = new List<ComponentVersionResponse>();

        foreach (var updater in _componentUpdaters)
        {
            try
            {
                var version = await updater.GetCurrentVersionAsync();
                versions.Add(version);
            }
            catch
            {
                versions.Add(new ComponentVersionResponse
                {
                    ComponentName = updater.ComponentName,
                    CurrentVersion = null
                });
            }
        }

        return Ok(versions);
    }
}
