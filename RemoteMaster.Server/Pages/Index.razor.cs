// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Collections.ObjectModel;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR.Client;
using Radzen;
using Radzen.Blazor;
using RemoteMaster.Server.Models;
using RemoteMaster.Server.Services;

namespace RemoteMaster.Server.Pages;

public partial class Index
{
    private List<Node> _entries;
    private Node _selectedNode;
    private Dictionary<string, (HubConnection agentConnection, HubConnection serverConnection)> _connections = new();

    [Inject]
    private DialogService DialogService { get; set; }

    [Inject]
    private DatabaseService DatabaseService { get; set; }

    [Inject]
    private NotificationService NotificationService { get; set; }

    [Inject]
    private ConnectionManager ConnectionManager { get; set; }

    protected override void OnInitialized()
    {
        _entries = new List<Node>();

        var folders = DatabaseService.GetFolders();

        foreach (var folder in folders.Where(f => f.Parent == null))
        {
            LoadChildren(folder);
            _entries.Add(folder);
        }

        DatabaseService.NodeAdded += OnNodeAdded;
    }

    private void LoadChildren(Folder folder)
    {
        var children = DatabaseService.GetFolders().Where(f => f.Parent == folder);

        foreach (var child in children)
        {
            folder.Children.Add(child);
            LoadChildren(child);
        }
    }

    private void OnNodeAdded(object? sender, Node node)
    {
        if (node is Folder folder)
        {
            if (folder.Parent == null)
            {
                _entries.Add(folder);
            }
            else
            {
                folder.Parent.Children.Add(folder);
            }
        }

        StateHasChanged();
    }

    private void LoadComputers(TreeExpandEventArgs args)
    {
        var node = args.Value as Node;
        var nodeId = node.NodeId;

        var children = new List<Node>();

        var subFolders = DatabaseService.GetFolders().Where(f => f.Parent == node);
        var computers = DatabaseService.GetComputersByFolderId(nodeId);

        children.AddRange(subFolders);
        children.AddRange(computers);

        args.Children.Data = children;
        args.Children.Text = GetTextForNode;
        args.Children.HasChildren = node => node is Folder && DatabaseService.GetFolders().Any(f => f.Parent == node);
        args.Children.Template = NodeTemplate;
    }

    private readonly RenderFragment<RadzenTreeItem> NodeTemplate = (context) => builder =>
    {
        if (context.Value is Computer computer)
        {
            builder.OpenComponent<RadzenIcon>(0);
            builder.AddAttribute(1, "Icon", "desktop_windows");
            builder.CloseComponent();

            builder.AddContent(2, $" {computer.Name} ({computer.IPAddress})");
        }
        else if (context.Value is Folder folder)
        {
            builder.OpenComponent<RadzenIcon>(0);
            builder.AddAttribute(1, "Icon", "folder");
            builder.CloseComponent();

            builder.AddContent(2, $" {folder.Name}");
        }
    };

    private string GetTextForNode(object data) => data as string;

    private async Task OnTreeChange(TreeEventArgs args)
    {
        _connections.Clear();

        var node = args.Value as Node;

        if (node is Folder)
        {
            _selectedNode = node;

            // foreach (var children in node.Children)
            // {
            //     if (children is Computer computer)
            //     {
            //         if (!_connections.ContainsKey(computer.IPAddress))
            //         {
            //             var agentConnection = ConnectionManager.CreateAgentConnection(computer.IPAddress);
            //             var serverConnection = ConnectionManager.CreateServerConnection(computer.IPAddress);
            //             _connections[computer.IPAddress] = (agentConnection, serverConnection);
            // 
            //             try
            //             {
            //                 await agentConnection.StartAsync();
            // 
            //                 Thread.Sleep(5000);
            // 
            //                 serverConnection.On<byte[]>("ReceiveThumbnail", async (thumbnailBytes) =>
            //                 {
            //                     if (thumbnailBytes != null && thumbnailBytes.Length > 0)
            //                     {
            //                         computer.Thumbnail = thumbnailBytes;
            //                         await InvokeAsync(StateHasChanged);
            //                     }
            //                 });
            // 
            //                 await serverConnection.StartAsync();
            //                 await serverConnection.InvokeAsync("ConnectAs", Intention.GetThumbnail);
            //             }
            //             catch
            //             {
            //                 //
            //             }
            //         }
            //     }
            // }
        }

        StateHasChanged();
    }
}