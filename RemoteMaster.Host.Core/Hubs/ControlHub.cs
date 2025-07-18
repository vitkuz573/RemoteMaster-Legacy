﻿// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Net;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.Connections.Features;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using RemoteMaster.Host.Core.Abstractions;
using RemoteMaster.Shared.Claims;
using RemoteMaster.Shared.DTOs;
using RemoteMaster.Shared.Enums;
using RemoteMaster.Shared.Models;
using SixLabors.ImageSharp;

namespace RemoteMaster.Host.Core.Hubs;

public class ControlHub(IAppState appState, IApplicationVersionProvider applicationVersionProvider, IViewerFactory viewerFactory, IScriptService scriptService, IInputService inputService, IPowerService powerService, IHardwareService hardwareService, IShutdownService shutdownService, IScreenCapturingService screenCapturingService, IWorkStationSecurityService workStationSecurityService, IScreenCastingService screenCastingService, IAudioStreamingService audioStreamingService, IOperatingSystemInformationService operatingSystemInformationService, ILogger<ControlHub> logger) : Hub<IControlClient>
{
    private static readonly List<string> ExcludedCodecs = ["image/tiff"];

    public async override Task OnConnectedAsync()
    {
        var user = Context.User;

        if (user == null)
        {
            var message = new Message("User is not authenticated.", Message.MessageSeverity.Error)
            {
                Meta = MessageMeta.AuthorizationError
            };

            await Clients.Caller.ReceiveMessage(message);
            
            Context.Abort();
            
            return;
        }

        var httpContext = Context.GetHttpContext();

        if (httpContext == null)
        {
            var message = new Message("HTTP context is unavailable.", Message.MessageSeverity.Error)
            {
                Meta = MessageMeta.ConnectionError
            };

            await Clients.Caller.ReceiveMessage(message);

            Context.Abort();

            return;
        }

        var query = httpContext.Request.Query;
        var ipAddress = httpContext.Connection.RemoteIpAddress ?? IPAddress.None;

        var userName = user.FindFirstValue(ClaimTypes.Name) ?? "UnknownUser";
        var role = user.FindFirstValue(ClaimTypes.Role) ?? "UnknownRole";
        var authenticationType = GetAuthenticationType(user);

        if (query.ContainsKey("thumbnail") && query["thumbnail"] == "true")
        {
            await HandleThumbnailRequestAsync(userName, role, ipAddress, authenticationType);

            return;
        }

        if (query.ContainsKey("screencast") && query["screencast"] == "true")
        {
            if (!query.TryGetValue("action", out var action) || string.IsNullOrEmpty(action))
            {
                logger.LogWarning("Missing or invalid action parameter for screencast from user {UserName} with IP {IpAddress}.", userName, ipAddress);

                var message = new Message("Missing or invalid action parameter for screencast.", Message.MessageSeverity.Error)
                {
                    Meta = MessageMeta.AuthorizationError
                };

                await Clients.Caller.ReceiveMessage(message);
                
                Context.Abort();
                
                return;
            }

            var hasControlClaim = user.HasClaim(c => c is { Type: "Connect", Value: "Control" });
            var hasViewClaim = user.HasClaim(c => c is { Type: "Connect", Value: "View" });

            var actionString = action.ToString();

            var isAuthorized = actionString switch
            {
                "control" when hasControlClaim => true,
                "view" when hasViewClaim => true,
                _ => false
            };

            if (!isAuthorized)
            {
                logger.LogWarning("Unauthorized attempt for action '{Action}' by user {UserName} with IP {IpAddress}.", action, userName, ipAddress);

                var message = new Message($"Unauthorized for action '{action}'.", Message.MessageSeverity.Error)
                {
                    Meta = MessageMeta.AuthorizationError
                };

                await Clients.Caller.ReceiveMessage(message);
                
                Context.Abort();
                
                return;
            }

            try
            {
                await HandleScreenCastRequestAsync(userName, role, ipAddress, authenticationType);
                
                logger.LogInformation("Screencast started for user {UserName} with IP {IpAddress}.", userName, ipAddress);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to start screencast for user {UserName} with IP {IpAddress}.", userName, ipAddress);

                var message = new Message("Failed to start screencast.", Message.MessageSeverity.Error)
                {
                    Meta = MessageMeta.ScreencastError
                };

                await Clients.Caller.ReceiveMessage(message);
                
                Context.Abort();
                
                return;
            }
        }

        var codecs = GetAvailableCodecs();

        await Clients.Caller.ReceiveAvailableCodecs(codecs);

        await base.OnConnectedAsync();
    }

    private static List<string> GetAvailableCodecs()
    {
        var imageFormats = Configuration.Default.ImageFormatsManager.ImageFormats.ToArray();

        var availableCodecs = imageFormats
            .Select(f => f.DefaultMimeType)
            .Where(m => !string.IsNullOrEmpty(m) && !ExcludedCodecs.Contains(m))
            .ToList();

        const string jpegMime = "image/jpeg";

        if (!availableCodecs.Contains(jpegMime))
        {
            return availableCodecs;
        }

        availableCodecs.Remove(jpegMime);
        availableCodecs.Insert(0, jpegMime);

        return availableCodecs;
    }

    private static string GetAuthenticationType(ClaimsPrincipal user)
    {
        var authenticationType = user.Identity?.AuthenticationType;

        if (string.IsNullOrEmpty(authenticationType))
        {
            authenticationType = user.FindFirstValue(CustomClaimTypes.AuthType);
        }

        return authenticationType ?? "Unknown";
    }

    private async Task HandleThumbnailRequestAsync(string userName, string role, IPAddress ipAddress, string authenticationType)
    {
        var tempViewer = viewerFactory.Create(Context, "ThumbnailGroup", Context.ConnectionId, userName, role, ipAddress, authenticationType);
        var added = await appState.TryAddViewerAsync(tempViewer);

        if (!added)
        {
            logger.LogError("Failed to add viewer for thumbnail request with ConnectionId {ConnectionId}.", Context.ConnectionId);
            
            var message = new Message("Failed to add viewer for thumbnail request.", Message.MessageSeverity.Error)
            {
                Meta = MessageMeta.ConnectionError
            };

            await Clients.Caller.ReceiveMessage(message);

            Context.Abort();

            return;
        }

        try
        {
            var thumbnail = screenCapturingService.GetThumbnail(tempViewer.ConnectionId);

            if (thumbnail != null)
            {
                await Clients.Caller.ReceiveThumbnail(thumbnail);
            }
            else
            {
                logger.LogError("Failed to generate thumbnail for ConnectionId {ConnectionId}.", tempViewer.ConnectionId);

                var message = new Message("Failed to generate thumbnail.", Message.MessageSeverity.Error)
                {
                    Meta = MessageMeta.ThumbnailError
                };

                await Clients.Caller.ReceiveMessage(message);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Exception occurred while generating thumbnail for ConnectionId {ConnectionId}.", tempViewer.ConnectionId);
            
            var message = new Message("An error occurred while generating the thumbnail.", Message.MessageSeverity.Error)
            {
                Meta = MessageMeta.ThumbnailError
            };

            await Clients.Caller.ReceiveMessage(message);
        }
        finally
        {
            var removed = await appState.TryRemoveViewerAsync(tempViewer.ConnectionId);

            if (!removed)
            {
                logger.LogWarning("Failed to remove temporary viewer for ConnectionId {ConnectionId}.", tempViewer.ConnectionId);
            }
        }

        await Clients.Caller.ReceiveCloseConnection();
    }

    private async Task HandleScreenCastRequestAsync(string userName, string role, IPAddress ipAddress, string authenticationType)
    {
        var viewer = viewerFactory.Create(Context, "Users", Context.ConnectionId, userName, role, ipAddress, authenticationType);
        var added = await appState.TryAddViewerAsync(viewer);

        if (!added)
        {
            logger.LogError("Failed to add viewer for screencast request with ConnectionId {ConnectionId}.", Context.ConnectionId);

            var message = new Message("Failed to add viewer for screencast request.", Message.MessageSeverity.Error)
            {
                Meta = MessageMeta.ConnectionError
            };

            await Clients.Caller.ReceiveMessage(message);

            Context.Abort();

            return;
        }

        try
        {
            screenCastingService.StartStreaming(viewer);

            var transportFeature = Context.Features.Get<IHttpTransportFeature>();
            var transportType = transportFeature?.TransportType.ToString() ?? "Unknown";

            await Clients.Caller.ReceiveTransportType(transportType);
            await Clients.Caller.ReceiveDotNetVersion(Environment.Version);

            var osBitness = Environment.Is64BitOperatingSystem ? "64-bit" : "32-bit";

            await Clients.Caller.ReceiveOperatingSystemVersion($"{operatingSystemInformationService.GetName()} ({osBitness})");
            await Clients.Caller.ReceiveHostVersion(applicationVersionProvider.GetVersionFromAssembly());
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Exception occurred while starting screencast for ConnectionId {ConnectionId}.", Context.ConnectionId);
            
            var message = new Message("An error occurred while starting the screencast.", Message.MessageSeverity.Error)
            {
                Meta = MessageMeta.ScreencastError
            };
            
            await Clients.Caller.ReceiveMessage(message);
            
            await appState.TryRemoveViewerAsync(viewer.ConnectionId);
            
            Context.Abort();
        }
    }

    public async override Task OnDisconnectedAsync(Exception? exception)
    {
        ExecuteActionForViewer(async viewer =>
        {
            logger.LogInformation("User {UserName} with role {Role} from IP {IpAddress} disconnected.", viewer.UserName, viewer.Role, viewer.IpAddress);
            
            await appState.TryRemoveViewerAsync(viewer.ConnectionId);
        });

        await base.OnDisconnectedAsync(exception);
    }

    [Authorize(Policy = "DisconnectClientPolicy")]
    public void DisconnectClient(ViewerDisconnectRequest disconnectRequest)
    {
        ArgumentNullException.ThrowIfNull(disconnectRequest);

        ExecuteActionForViewer(async viewer =>
        {
            await Clients.Client(disconnectRequest.ConnectionId).ReceiveDisconnected(disconnectRequest.Reason);
            
            await appState.TryRemoveViewerAsync(viewer.ConnectionId);
        });
    }

    [Authorize(Policy = "HandleInputPolicy")]
    public void HandleMouseInput(MouseInputDto dto)
    {
        ExecuteActionForViewer(viewer => inputService.HandleMouseInput(dto, viewer.ConnectionId));
    }

    [Authorize(Policy = "HandleInputPolicy")]
    public void HandleKeyboardInput(KeyboardInputDto dto)
    {
        ExecuteActionForViewer(viewer => inputService.HandleKeyboardInput(dto, viewer.ConnectionId));
    }

    [Authorize(Policy = "ChangeScreenPolicy")]
    public void ChangeSelectedScreen(string displayName)
    {
        ExecuteActionForViewer(viewer =>
        {
            var screen = screenCapturingService.FindScreenByName(displayName);

            if (screen != null)
            {
                viewer.CapturingContext.SelectedScreen = screen;
            }
            else
            {
                logger.LogError("Screen with name '{DisplayName}' not found for connection ID {ConnectionId}.", displayName, Context.ConnectionId);
            }
        });
    }

    [Authorize(Policy = "ToggleInputPolicy")]
    public void ToggleInput(bool inputEnabled)
    {
        inputService.InputEnabled = inputEnabled;
    }

    [Authorize(Policy = "BlockUserInputPolicy")]
    public void BlockUserInput(bool blockInput)
    {
        inputService.BlockUserInput = blockInput;
    }

    [Authorize(Policy = "SetFrameRatePolicy")]
    public void SetFrameRate(int frameRate)
    {
        ExecuteActionForViewer(viewer => viewer.CapturingContext.FrameRate = frameRate);
    }

    [Authorize(Policy = "SetImageQualityPolicy")]
    public void SetImageQuality(int quality)
    {
        ExecuteActionForViewer(viewer => viewer.CapturingContext.ImageQuality = quality);
    }

    [Authorize(Policy = "SetCodecPolicy")]
    public void SetCodec(string codec)
    {
        ExecuteActionForViewer(viewer => viewer.CapturingContext.SelectedCodec = codec);
    }

    [Authorize(Policy = "ToggleIsCursorVisiblePolicy")]
    public void ToggleIsCursorVisible(bool isCursorVisible)
    {
        ExecuteActionForViewer(viewer => viewer.CapturingContext.IsCursorVisible = isCursorVisible);
    }

    [Authorize(Policy = "TerminateHostPolicy")]
    public void TerminateHost()
    {
        shutdownService.ImmediateShutdown();
    }

    [Authorize(Policy = "RebootHostPolicy")]
    [HubMethodName("RebootHost")]
    public async Task RebootHostAsync(PowerActionRequest powerActionRequest)
    {
        await powerService.RebootAsync(powerActionRequest);
    }

    [Authorize(Policy = "ShutdownHostPolicy")]
    [HubMethodName("ShutdownHost")]
    public async Task ShutdownHostAsync(PowerActionRequest powerActionRequest)
    {
        await powerService.ShutdownAsync(powerActionRequest);
    }

    [Authorize(Policy = "SetMonitorStatePolicy")]
    public void SetMonitorState(MonitorState state)
    {
        hardwareService.SetMonitorState(state);
    }

    [Authorize(Policy = "ExecuteScriptPolicy")]
    public void ExecuteScript(ScriptExecutionRequest scriptExecutionRequest)
    {
        scriptService.ExecuteAsync(scriptExecutionRequest);
    }

    [Authorize(Policy = "LockWorkStationPolicy")]
    public void LockWorkStation()
    {
        workStationSecurityService.LockWorkStationDisplay();
    }

    [Authorize(Policy = "LogOffUserPolicy")]
    public void LogOffUser(bool force)
    {
        workStationSecurityService.LogOffUser(force);
    }

    [Authorize(Policy = "AudioStreamingPolicy")]
    public void StartAudioStreaming()
    {
        ExecuteActionForViewer(audioStreamingService.StartStreaming);
    }

    [Authorize(Policy = "AudioStreamingPolicy")]
    public void StopAudioStreaming()
    {
        ExecuteActionForViewer(audioStreamingService.StopStreaming);
    }

    private void ExecuteActionForViewer(Action<IViewer> action)
    {
        if (appState.TryGetViewer(Context.ConnectionId, out var viewer))
        {
            if (viewer != null)
            {
                action(viewer);
            }
        }
        else
        {
            logger.LogError("Failed to find a viewer for connection ID {ConnectionId}", Context.ConnectionId);
        }
    }
}
