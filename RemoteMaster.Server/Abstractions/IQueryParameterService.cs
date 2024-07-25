// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using RemoteMaster.Shared.Models;

namespace RemoteMaster.Server.Abstractions;

public interface IQueryParameterService
{
    Result<T> GetParameter<T>(string key, T defaultValue);

    Result UpdateParameter(string key, object value);
}