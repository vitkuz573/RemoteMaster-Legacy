// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using Microsoft.AspNetCore.Components;

namespace RemoteMaster.Server.Components;

public partial class RegistryKeyNode
{
    [Parameter]
    public string KeyName { get; set; } = string.Empty;

    [Parameter]
    public string ParentKey { get; set; } = string.Empty;

    [Parameter]
    public EventCallback<string> OnSubKeyExpand { get; set; }

    [Parameter]
    public EventCallback<string> OnKeySelect { get; set; }

    private bool IsExpanded { get; set; } = false;

#pragma warning disable CA2227
    public List<string> SubKeys { get; set; } = [];
#pragma warning restore CA2227

    private readonly List<RegistryKeyNode> _childNodes = [];

    public string KeyFullPath => string.IsNullOrEmpty(ParentKey) ? KeyName : $"{ParentKey}\\{KeyName}";

    private async Task ToggleExpand()
    {
        IsExpanded = !IsExpanded;

        if (IsExpanded)
        {
            await OnSubKeyExpand.InvokeAsync(KeyFullPath);
        }

        await InvokeAsync(StateHasChanged);
    }

    private async Task SelectThisKey()
    {
        await OnKeySelect.InvokeAsync(KeyFullPath);
    }

    public void SetSubKeys(IEnumerable<string> subKeys)
    {
        SubKeys = subKeys.ToList();

        Logger.LogInformation("SubKeys count: {Count}", SubKeys.Count);

        StateHasChanged();
    }

    public RegistryKeyNode? FindNodeByKey(string fullPath)
    {
        if (KeyFullPath.Equals(fullPath, StringComparison.OrdinalIgnoreCase))
        {
            return this;
        }

        foreach (var childNode in _childNodes)
        {
            var foundNode = childNode.FindNodeByKey(fullPath);

            if (foundNode != null)
            {
                return foundNode;
            }
        }

        return null;
    }

    protected override void OnAfterRender(bool firstRender)
    {
        if (firstRender && !_childNodes.Contains(this))
        {
            _childNodes.Add(this);
        }
    }
}
