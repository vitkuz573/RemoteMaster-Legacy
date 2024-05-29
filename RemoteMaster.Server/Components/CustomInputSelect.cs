// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using Microsoft.AspNetCore.Components;

namespace RemoteMaster.Server.Components;

public partial class CustomInputSelect<TValue> : ComponentBase
{
#pragma warning disable CA2227
    [Parameter]
    public List<SelectOption<TValue>> Options { get; set; } = [];
#pragma warning restore CA2227

    [Parameter]
    public EventCallback<List<TValue>> SelectedValuesChanged { get; set; }

    private List<TValue> SelectedValues { get; set; } = [];

    private void OnChange(ChangeEventArgs e)
    {
        var selectedOptions = e.Value as IEnumerable<string>;

        if (selectedOptions != null)
        {
            SelectedValues = selectedOptions.Select(value => (TValue)Convert.ChangeType(value, typeof(TValue))).ToList();
            SelectedValuesChanged.InvokeAsync(SelectedValues);
        }
    }
}
