// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using Microsoft.AspNetCore.Mvc;
using RemoteMaster.Updater.Abstractions;
using RemoteMaster.Updater.Models;

namespace RemoteMaster.Updater.Controllers;

[Route("api/[controller]")]
[ApiController]
public class UpdateController : ControllerBase
{
    private readonly IEnumerable<IComponentUpdater> _componentUpdaters;

    public UpdateController(IEnumerable<IComponentUpdater> componentUpdaters)
    {
        _componentUpdaters = componentUpdaters;
    }

    [HttpGet("check")]
    public async Task<IActionResult> CheckForUpdates([FromQuery] string sharedFolder, [FromQuery] string login, [FromQuery] string password)
    {
        var updateResults = new List<UpdateResponse>();

        foreach (var updater in _componentUpdaters)
        {
            var response = new UpdateResponse { ComponentName = updater.ComponentName };

            try
            {
                var result = await updater.IsUpdateAvailableAsync(sharedFolder, login, password);
                response.CurrentVersion = result.CurrentVersion;
                response.AvailableVersion = result.AvailableVersion;
                response.IsUpdateAvailable = result.IsUpdateAvailable;
                response.Message = result.IsUpdateAvailable ? "Update is available." : "No updates available.";
            }
            catch (Exception ex)
            {
                response.Error = new ErrorResponse
                {
                    ErrorMessage = ex.Message,
                    StackTrace = ex.StackTrace
                };
            }

            updateResults.Add(response);
        }

        return Ok(updateResults);
    }

    [HttpPost("update")]
    public async Task<IActionResult> Update([FromQuery] string sharedFolder, [FromQuery] string login, [FromQuery] string password)
    {
        var updateResults = new List<UpdateResponse>();

        foreach (var updater in _componentUpdaters)
        {
            var response = new UpdateResponse { ComponentName = updater.ComponentName };

            try
            {
                var updateCheckResponse = await updater.IsUpdateAvailableAsync(sharedFolder, login, password);

                if (updateCheckResponse.IsUpdateAvailable)
                {
                    await updater.UpdateAsync(sharedFolder, login, password);
                    response.Message = "Update completed successfully.";
                }
                else
                {
                    response.Message = "No updates available.";
                }

                response.CurrentVersion = updateCheckResponse.CurrentVersion;
            }
            catch (Exception ex)
            {
                response.Error = new ErrorResponse
                {
                    ErrorMessage = ex.Message,
                    StackTrace = ex.StackTrace
                };
            }

            updateResults.Add(response);
        }

        return Ok(updateResults);
    }
}