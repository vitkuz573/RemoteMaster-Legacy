// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

namespace RemoteMaster.Server.Models;

public record NotificationMessage(string Id, string Title, string Except, string Category, DateTime PublishDate, string ImgUrl, IEnumerable<NotificationAuthor> Authors, Type ContentComponent);