// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using Microsoft.AspNetCore.Components;
using RemoteMaster.Server.Components.Library.Abstractions;

namespace RemoteMaster.Server.Components.Library;

public class DialogReference : IDialogReference
{
    public Guid DialogId { get; private set; }

    public RenderFragment Content { get; private set; }

    public DialogInstance Instance { get; private set; }

    public DialogReference(Guid dialogId, RenderFragment content, DialogInstance instance)
    {
        DialogId = dialogId;
        Content = content;
        Instance = instance;
    }
}
