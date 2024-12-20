// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

namespace RemoteMaster.Shared.Models;

public static class MessageMeta
{
    public const string ProcessIdInformation = "pidInformation";
    public const string ConnectionError = "connectionError";
    public const string AuthorizationError = "authorizationError";
    public const string ScreencastError = "screencastError";
    public const string ThumbnailError = "thumbnailError";
}
