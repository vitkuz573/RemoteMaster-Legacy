// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Collections.ObjectModel;
using Microsoft.AspNetCore.Components;
using Radzen;
using Radzen.Blazor;
using RemoteMaster.Client.Models;
using RemoteMaster.Client.Services;

namespace RemoteMaster.Client.Pages;

public partial class Index
{
    private List<Node> _entries;

    private Node _selectedNode;

    [Inject]
    private DialogService DialogService { get; set; }

    [Inject]
    private DatabaseService DatabaseService { get; set; }

    [Inject]
    private ActiveDirectoryService ActiveDirectoryService { get; set; }

    [Inject]
    private NotificationService NotificationService { get; set; }

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

    private async Task GetComputersFromAD()
    {
        try
        {
            var domainComputers = await ActiveDirectoryService.FetchComputers();

            var adNodes = new ObservableCollection<Node>(domainComputers.Select(ou =>
            {
                var folder = new Folder(ou.Key);

                foreach (var computer in ou.Value)
                {
                    folder.Children.Add(computer);
                }

                return (Node)folder;
            }).ToList());
        }
        catch (Exception ex)
        {
            NotificationService.Notify(new NotificationMessage
            {
                Severity = NotificationSeverity.Error,
                Summary = "Error",
                Detail = $"Failed to get computers from AD: {ex.Message}",
                Duration = 4000
            });
        }
    }

    public async Task OpenNewFolder()
    {
        await DialogService.OpenAsync<NewFolderPage>("New Folder", options: new DialogOptions
        {
            Draggable = true
        });
    }

    public async Task OpenNewComputer()
    {
        await DialogService.OpenAsync<NewComputerPage>("New Computer", options: new DialogOptions
        {
            Draggable = true
        });
    }

    private void OnTreeChange(TreeEventArgs args)
    {
        var node = args.Value as Node;

        if (node is Folder)
        {
            _selectedNode = node;
        }

        StateHasChanged();
    }
}