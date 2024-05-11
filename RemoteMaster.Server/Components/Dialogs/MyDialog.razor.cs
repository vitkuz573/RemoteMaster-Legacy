// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using Microsoft.AspNetCore.Components;
using RemoteMaster.Server.Components.Library;

namespace RemoteMaster.Server.Components.Dialogs;

public partial class MyDialog : ComponentBase
{
    [CascadingParameter]
    private DialogInstance Dialog { get; set; }

    [Parameter]
    public string Title { get; set; }

#pragma warning disable
    private void OnCancel()
    {
        Dialog.Close();
    }

    private void OnConfirm()
    {
        Dialog.Close();
    }
}
