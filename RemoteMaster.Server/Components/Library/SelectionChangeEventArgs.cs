// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Drawing;

namespace RemoteMaster.Server.Components.Library;

public class SelectionChangeEventArgs : EventArgs
{
    public RectangleF SelectionRectangle { get; set; }

#pragma warning disable CA2227
    public List<string> SelectedElementIds { get; set; }
#pragma warning restore CA2227
}

