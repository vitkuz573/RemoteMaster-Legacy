// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

namespace RemoteMaster.Host.Core.Abstractions;

public interface IClipboardService
{
    /// <summary>
    /// Retrieves text from the system clipboard.
    /// </summary>
    /// <returns>Text from the clipboard or null if no text is available.</returns>
    Task<string?> GetTextAsync();

    /// <summary>
    /// Sets text to the system clipboard.
    /// </summary>
    /// <param name="text">Text to set to the clipboard.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task SetTextAsync(string text);
}
