// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Text;
using RemoteMaster.Server.Components.Library.Models;

namespace RemoteMaster.Server.Components.Library.Utilities;

public class CssClassBuilder
{
    private readonly List<string> _baseClasses = [];
    private readonly List<CssClassCondition> _conditions = [];
    private readonly Dictionary<string, CssClassBuilder> _mediaQueries = [];

    public CssClassBuilder AddBase(string className)
    {
        _baseClasses.Add(className);
        
        return this;
    }

    public CssClassBuilder Add(string className, bool condition = true, bool important = false)
    {
        _conditions.Add(new CssClassCondition(className, condition, important));
        
        return this;
    }

    public CssClassBuilder Remove(string className)
    {
        _conditions.RemoveAll(c => c.ClassName == className);

        return this;
    }

    public CssClassBuilder Toggle(string className, bool condition)
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

    public CssClassBuilder AddMediaQuery(string query, Action<CssClassBuilder> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);

        var builder = new CssClassBuilder();

        configure(builder);
        _mediaQueries[query] = builder;

        return this;
    }

    public string Build()
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

        foreach (var (query, innerBuilder) in _mediaQueries)
        {
            if (builder.Length > 0)
            {
                builder.Append(' ');
            }

            builder.Append($"@media {query} {{ {innerBuilder.Build()} }}");
        }

        return builder.ToString().Trim();
    }
}
