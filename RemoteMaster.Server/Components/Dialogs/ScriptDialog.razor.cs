// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.FluentUI.AspNetCore.Components;
using RemoteMaster.Server.Abstractions;
using RemoteMaster.Server.Models;
using RemoteMaster.Shared.Models;

namespace RemoteMaster.Server.Components.Dialogs;

#pragma warning disable CA2227

public partial class ScriptDialog
{
    [CascadingParameter]
    public FluentDialog Dialog { get; set; } = default!;

    [Parameter]
    public Dictionary<Computer, HubConnection> Content { get; set; } = default!;

    [Inject]
    public IComputerCommandService ComputerCommandService { get; set; } = default!;

    private FluentInputFile? _scriptByStream;
    private int? _progressPercent;
    private string? _progressTitle;
    private readonly string _accept;
    private string? _shell;
    private string? _file;

    public ScriptDialog()
    {
        var acceptList = new List<string>
        {
            ".ps1",
            ".bat",
            ".cmd"
        };

        _accept = string.Join(", ", acceptList);

        _scriptByStream = default;
    }

    private async Task OnFileUploadedAsync(FluentInputFileEventArgs file)
    {
        _progressPercent = file.ProgressPercent;
        _progressTitle = file.ProgressTitle;

        _file = Path.GetTempFileName() + file.Name;

        // Write to the FileStream
        // See other samples: https://docs.microsoft.com/en-us/aspnet/core/blazor/file-uploads
        await using FileStream fs = new(_file, FileMode.Create);
        await file.Stream!.CopyToAsync(fs);
        await file.Stream!.DisposeAsync();
    }

    private void OnCompleted(IEnumerable<FluentInputFileEventArgs> files)
    {
        _progressPercent = _scriptByStream!.ProgressPercent;
        _progressTitle = _scriptByStream!.ProgressTitle;

        _shell = Path.GetExtension(_file) switch
        {
            ".ps1" => Shell.PowerShell.ToString(),
            ".bat" => Shell.Cmd.ToString(),
            ".cmd" => Shell.Cmd.ToString()
        };
    }

    private async Task Execute()
    {
        await Dialog.CloseAsync();
    }
}
