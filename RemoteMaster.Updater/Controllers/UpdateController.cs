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

    [HttpGet]
    public async Task<IActionResult> CheckForUpdates([FromQuery] UpdateRequest updateRequest)
    {
        if (updateRequest == null || string.IsNullOrWhiteSpace(updateRequest.SharedFolder) || string.IsNullOrWhiteSpace(updateRequest.Login) || string.IsNullOrWhiteSpace(updateRequest.Password))
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
                var result = await updater.IsUpdateAvailableAsync(updateRequest.SharedFolder, updateRequest.Login, updateRequest.Password);
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

    [HttpPost]
    public async Task<IActionResult> Update([FromBody] UpdateRequest updateRequest)
    {
        if (updateRequest == null || string.IsNullOrWhiteSpace(updateRequest.SharedFolder) || string.IsNullOrWhiteSpace(updateRequest.Login) || string.IsNullOrWhiteSpace(updateRequest.Password))
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
            var response = new UpdateResponse
            {
                ComponentName = updater.ComponentName
            };
            
            try
            {
                var updateCheckResponse = await updater.IsUpdateAvailableAsync(updateRequest.SharedFolder, updateRequest.Login, updateRequest.Password);

                if (updateCheckResponse.IsUpdateAvailable)
                {
                    await updater.UpdateAsync(updateRequest.SharedFolder, updateRequest.Login, updateRequest.Password);
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

    [HttpGet("versions")]
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