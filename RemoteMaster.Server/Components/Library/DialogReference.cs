// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using Microsoft.AspNetCore.Components;
using RemoteMaster.Server.Components.Library.Abstractions;

namespace RemoteMaster.Server.Components.Library;

public class DialogReference(Guid dialogId, RenderFragment content, DialogInstance instance) : IDialogReference
{
    public Guid Id { get; private set; } = dialogId;

    public RenderFragment Content { get; private set; } = content;

    public DialogInstance Instance { get; private set; } = instance;
}
