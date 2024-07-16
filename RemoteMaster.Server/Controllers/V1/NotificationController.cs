// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using RemoteMaster.Server.Abstractions;
using RemoteMaster.Shared.Models;

namespace RemoteMaster.Server.Controllers.V1;

[ApiController]
[Route("api/[controller]")]
[ApiVersion("1.0")]
[Consumes("application/vnd.remotemaster.v1+json")]
[Produces("application/vnd.remotemaster.v1+json")]
public class NotificationController(INotificationService notificationService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IDictionary<NotificationMessage, bool>>), 200)]
    public async Task<IActionResult> GetNotifications()
    {
        var notifications = await notificationService.GetNotifications();
        var response = ApiResponse<IDictionary<NotificationMessage, bool>>.Success(notifications, "Notifications retrieved successfully.");
        return Ok(response);
    }

    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ApiResponse<NotificationMessage>), 200)]
    [ProducesResponseType(typeof(ApiResponse<string>), 404)]
    public async Task<IActionResult> GetNotificationById(string id)
    {
        var message = await notificationService.GetMessageById(id);

        if (message == null)
        {
            var problemDetails = new ProblemDetails
            {
                Title = "Notification not found",
                Detail = "The notification with the specified ID was not found.",
                Status = StatusCodes.Status404NotFound
            };

            var errorResponse = ApiResponse<string>.Failure(problemDetails, StatusCodes.Status404NotFound);

            return NotFound(errorResponse);
        }

        var response = ApiResponse<NotificationMessage>.Success(message, "Notification retrieved successfully.");

        return Ok(response);
    }

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<string>), 201)]
    [ProducesResponseType(typeof(ApiResponse<string>), 400)]
    public async Task<IActionResult> AddNotification([FromBody] NotificationMessage message)
    {
        if (message == null || string.IsNullOrWhiteSpace(message.Id))
        {
            var problemDetails = new ProblemDetails
            {
                Title = "Invalid request",
                Detail = "The provided notification message is invalid.",
                Status = StatusCodes.Status400BadRequest
            };
            var errorResponse = ApiResponse<string>.Failure(problemDetails);

            return BadRequest(errorResponse);
        }

        await notificationService.AddNotification(message);
        var response = ApiResponse<string>.Success(message.Id, "Notification added successfully.");

        return CreatedAtAction(nameof(GetNotificationById), new { id = message.Id }, response);
    }

    [HttpPut("{id}/read")]
    [ProducesResponseType(typeof(ApiResponse<bool>), 200)]
    public async Task<IActionResult> MarkAsRead(string id)
    {
        await notificationService.MarkNotificationsAsRead(id);
        var response = ApiResponse<bool>.Success(true, "Notification marked as read successfully.");

        return Ok(response);
    }

    [HttpGet("areNewAvailable")]
    [ProducesResponseType(typeof(ApiResponse<bool>), 200)]
    public async Task<IActionResult> AreNewNotificationsAvailable()
    {
        var areNewAvailable = await notificationService.AreNewNotificationsAvailable();
        var response = ApiResponse<bool>.Success(areNewAvailable, "Checked for new notifications successfully.");

        return Ok(response);
    }

    [HttpPut("readAll")]
    [ProducesResponseType(typeof(ApiResponse<bool>), 200)]
    public async Task<IActionResult> MarkAllAsRead()
    {
        await notificationService.MarkNotificationsAsRead();
        var response = ApiResponse<bool>.Success(true, "All notifications marked as read successfully.");

        return Ok(response);
    }
}
