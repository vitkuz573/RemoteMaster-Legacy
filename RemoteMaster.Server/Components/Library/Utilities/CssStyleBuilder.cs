// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

namespace RemoteMaster.Server.Components.Library.Utilities;

public class CssStyleBuilder
{
    private readonly Dictionary<string, string> _styles = [];

    public CssStyleBuilder Add(string property, string value)
    {
        _styles[property] = value;

        return this;
    }

    public string Build()
    {
        return string.Join("; ", _styles.Select(kvp => $"{kvp.Key}: {kvp.Value}"));
    }
}