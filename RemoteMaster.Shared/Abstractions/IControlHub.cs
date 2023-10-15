// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using RemoteMaster.Shared.Dtos;
using RemoteMaster.Shared.Models;

namespace RemoteMaster.Shared.Abstractions;

public interface IControlHub
{
    Task ConnectAs(Intention intention);

    Task SendMouseCoordinates(MouseMoveDto dto);

    Task SendMouseButton(MouseClickDto dto);

    Task SendMouseWheel(MouseWheelDto dto);

    Task SendKeyboardInput(KeyboardKeyDto dto);

    Task SendSelectedScreen(string displayName);

    Task SetInputEnabled(bool inputEnabled);

    Task SetQuality(int quality);

    Task SetTrackCursor(bool trackCursor);

    Task KillHost();

    Task RebootComputer(string message, int timeout, bool forceAppsClosed);

    Task ShutdownComputer(string message, int timeout, bool forceAppsClosed);

    Task StartScreenRecording(string outputPath);

    Task StopScreenRecording();

    Task SetMonitorState(MonitorState state);

    Task ExecuteScript(string scriptContent, string shellType);

    Task SetPSExecRules(bool enable);

    Task JoinGroup(string groupName);

    Task LeaveGroup(string groupName);

    Task SendCommandToService(string command);
}