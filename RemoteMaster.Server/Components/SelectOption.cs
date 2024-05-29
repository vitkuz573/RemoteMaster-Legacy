// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

namespace RemoteMaster.Server.Components;

public class SelectOption<TValue>
{
    public TValue Value { get; set; }

    public string Text { get; set; }
}
