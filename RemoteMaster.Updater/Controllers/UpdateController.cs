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
    public async Task<IActionResult> CheckForUpdates([FromQuery] string? sharedFolder, [FromQuery] string? login, [FromQuery] string? password)
    {
        if (string.IsNullOrWhiteSpace(sharedFolder) || string.IsNullOrWhiteSpace(login) || string.IsNullOrWhiteSpace(password))
        {
            var errorResponse = new ErrorResponse
            {
                ErrorMessage = "Required parameters (sharedFolder, login, password) are missing."
            };

            return BadRequest(errorResponse);
        }

        var updateResults = new List<UpdateResponse>();

        foreach (var updater in _componentUpdaters)
        {
            try
            {
                var result = await updater.IsUpdateAvailableAsync(sharedFolder, login, password);
                updateResults.Add(result);
            }
            catch (Exception ex)
            {
                var response = new UpdateResponse
                {
                    ComponentName = updater.ComponentName,
                    Error = new ErrorResponse
                    {
                        ErrorMessage = ex.Message,
                        StackTrace = ex.StackTrace
                    }
                };
                updateResults.Add(response);
            }
        }

        return Ok(updateResults);
    }

    [HttpPost("update")]
    public async Task<IActionResult> Update([FromQuery] string? sharedFolder = null, [FromQuery] string? login = null, [FromQuery] string? password = null)
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