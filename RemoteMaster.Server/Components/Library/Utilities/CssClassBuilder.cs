// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Text;
using RemoteMaster.Server.Components.Library.Models;

namespace RemoteMaster.Server.Components.Library.Utilities;

public class CssClassBuilder
{
    private readonly HashSet<string> _baseClasses = [];
    private readonly List<CssClassCondition> _conditions = [];

    public CssClassBuilder AddBase(string classNames)
    {
        ArgumentNullException.ThrowIfNull(classNames);

        var classes = classNames.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        
        foreach (var className in classes)
        {
            _baseClasses.Add(className);
        }

        return this;
    }

    public CssClassBuilder Add(string className, bool condition = true)
    {
        var existingCondition = _conditions.FirstOrDefault(c => c.ClassName == className);
        
        if (existingCondition != null)
        {
            existingCondition.Condition = condition;
        }
        else
        {
            _conditions.Add(new CssClassCondition(className, condition));
        }

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

    public string Build()
    {
        var builder = new StringBuilder(string.Join(" ", _baseClasses));

        foreach (var condition in _conditions.Where(c => c.Condition))
        {
            if (builder.Length > 0)
            {
                builder.Append(' ');
            }

            builder.Append(condition.ClassName);
        }

        return builder.ToString().Trim();
    }
}
