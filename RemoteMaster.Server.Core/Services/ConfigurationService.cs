// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using RemoteMaster.Client.Core.Abstractions;
using RemoteMaster.Shared.Dtos;

namespace RemoteMaster.Client.Core.Services;

public class ConfigurationService : IConfigurationService
{
    private readonly IInputService _inputService;
    private readonly IScreenCapturerService _screenCapturerService;

    public ConfigurationService(IInputService inputService, IScreenCapturerService screenCapturerService)
    {
        _inputService = inputService;
        _screenCapturerService = screenCapturerService;
    }

    public ServerConfigurationDto Configure()
    {
        return new ServerConfigurationDto
        {
            InputEnabled = _inputService.InputEnabled,
            TrackCursor = _screenCapturerService.TrackCursor,
            ImageQuality = _screenCapturerService.Quality
        };
    }
}
