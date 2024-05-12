// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Linq.Expressions;

namespace RemoteMaster.Server.Components.Library.Models;

public class DialogParameters<T> : DialogParameters
{
    public void Add<TParam>(Expression<Func<T, TParam>> propertyExpression, TParam value)
    {
        ArgumentNullException.ThrowIfNull(propertyExpression);
        
        if (propertyExpression.Body is not MemberExpression memberExpression)
        {
            throw new ArgumentException($"Argument '{nameof(propertyExpression)}' must be a '{nameof(MemberExpression)}'");
        }

        Add(memberExpression.Member.Name, value);
    }

    public TParam Get<TParam>(Expression<Func<T, TParam>> propertyExpression)
    {
        ArgumentNullException.ThrowIfNull(propertyExpression);

        if (propertyExpression.Body is not MemberExpression memberExpression)
        {
            throw new ArgumentException($"Argument '{nameof(propertyExpression)}' must be a '{nameof(MemberExpression)}'");
        }

        return Get<TParam>(memberExpression.Member.Name);
    }

    public TParam? TryGet<TParam>(Expression<Func<T, TParam>> propertyExpression)
    {
        ArgumentNullException.ThrowIfNull(propertyExpression);

        if (propertyExpression.Body is not MemberExpression memberExpression)
        {
            throw new ArgumentException($"Argument '{nameof(propertyExpression)}' must be a '{nameof(MemberExpression)}'");
        }

        return TryGet<TParam>(memberExpression.Member.Name);
    }
}