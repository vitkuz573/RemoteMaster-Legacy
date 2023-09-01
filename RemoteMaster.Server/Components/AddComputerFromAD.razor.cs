// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Collections.ObjectModel;
using RemoteMaster.Server.Models;

namespace RemoteMaster.Server.Components;

public partial class AddComputerFromAD
{
    // private BitModal _modalRef;
    private bool IsOpen = false;
    private ObservableCollection<Node> _adNodes = new();
    private IList<Node> _expandedNodes = new List<Node>();
    private Node _selectedNode;

    public void Show(ObservableCollection<Node> adNodes)
    {
        _adNodes = adNodes;
        IsOpen = true;
        StateHasChanged();
    }
}
