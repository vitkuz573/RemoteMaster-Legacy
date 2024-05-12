// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Text;
using RemoteMaster.Server.Components.Library.Models;

namespace RemoteMaster.Server.Components.Library.Utilities;

public class CssBuilder
{
    private readonly List<string> _baseClasses = [];
    private readonly List<CssClassCondition> _conditions = [];
    private readonly Dictionary<string, CssBuilder> _mediaQueries = [];
    private readonly Dictionary<string, string> _styles = [];

    public CssBuilder AddBase(string className)
    {
        _baseClasses.Add(className);
       
        return this;
    }

    public CssBuilder Add(string className, bool condition = true, bool important = false)
    {
        if (condition)
        {
            _conditions.Add(new CssClassCondition(className, condition, important));
        }
        
        return this;
    }

    public CssBuilder Remove(string className)
    {
        _conditions.RemoveAll(c => c.ClassName == className);

        return this;
    }

    public CssBuilder Toggle(string className, bool condition)
    {
        var existingCondition = _conditions.FirstOrDefault(c => c.ClassName == className);
        
        if (existingCondition != null)
        {
            existingCondition.Condition = condition;
        }
        else
        {
            Add(className, condition);
        }

        return this;
    }

    public CssBuilder AddStyle(string property, string value)
    {
        _styles[property] = value;

        return this;
    }

    public CssBuilder AddMediaQuery(string query, Action<CssBuilder> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);

        var builder = new CssBuilder();

        configure(builder);
        _mediaQueries[query] = builder;

        return this;
    }

    public string BuildClasses()
    {
        var builder = new StringBuilder();
        builder.Append(string.Join(" ", _baseClasses.Distinct()));

        foreach (var condition in _conditions)
        {
            if (condition.Condition)
            {
                if (builder.Length > 0)
                {
                    builder.Append(' ');
                }

                builder.Append(condition.ClassName);

                if (condition.Important)
                {
                    builder.Append(" !important");
                }
            }
        }

        return builder.ToString().Trim();
    }

    public string BuildStyles()
    {
        return string.Join("; ", _styles.Select(kvp => $"{kvp.Key}: {kvp.Value}"));
    }

    public string Build()
    {
        var classes = BuildClasses();
        var styles = BuildStyles();

        return $"{classes}{(classes.Length > 0 && styles.Length > 0 ? "; " : "")}{styles}";
    }
}

