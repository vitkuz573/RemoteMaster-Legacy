// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Text;

namespace RemoteMaster.Server.Components.Library.Utilities;

public class CssStyleBuilder
{
    private readonly Dictionary<string, string> _styles = [];

    public CssStyleBuilder Add(string property, string value, bool important = false)
    {
        _styles[property] = value + (important ? " !important" : "");
       
        return this;
    }

    public CssStyleBuilder Remove(string property)
    {
        _styles.Remove(property);

        return this;
    }

    public string Build()
    {
        var builder = new StringBuilder();

        foreach (var kvp in _styles)
        {
            if (builder.Length > 0)
            {
                builder.Append("; ");
            }

            builder.Append($"{kvp.Key}: {kvp.Value}");
        }

        return builder.ToString();
    }
}
