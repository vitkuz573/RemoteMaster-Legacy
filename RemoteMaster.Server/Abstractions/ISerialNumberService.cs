// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using RemoteMaster.Shared.Models;

namespace RemoteMaster.Server.Abstractions;

/// <summary>
/// Defines a service for generating serial numbers.
/// </summary>
public interface ISerialNumberService
{
    /// <summary>
    /// Generates a serial number.
    /// </summary>
    /// <returns>A <see cref="Result{T}"/> containing the generated serial number or an error message.</returns>
    Result<byte[]> GenerateSerialNumber();
}
