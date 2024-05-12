// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Text;
using RemoteMaster.Server.Components.Library.Models;

namespace RemoteMaster.Server.Components.Library.Utilities;

public class CssStyleBuilder
{
    private readonly Dictionary<string, CssStyleCondition> _styles = [];

    public CssStyleBuilder Add(string property, string value, bool condition = true, bool important = false)
    {
        if (condition)
        {
            if (_styles.ContainsKey(property))
            {
                _styles[property] = new CssStyleCondition(property, value + (important ? " !important" : ""), condition);
            }
            else
            {
                _styles.Add(property, new CssStyleCondition(property, value + (important ? " !important" : ""), condition));
            }
        }
        else
        {
            _styles.Remove(property);
        }

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

        foreach (var style in _styles.Values)
        {
            if (style.Condition)
            {
                if (builder.Length > 0)
                {
                    builder.Append("; ");
                }

                builder.Append($"{style.Property}: {style.Value}");
            }
        }

        return builder.ToString().TrimEnd();
    }
}
