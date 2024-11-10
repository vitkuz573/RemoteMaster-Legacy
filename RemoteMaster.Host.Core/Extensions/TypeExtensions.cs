// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

namespace RemoteMaster.Host.Core.Extensions;

public static class TypeExtensions
{
    public static string GetFriendlyName(this Type type)
    {
        ArgumentNullException.ThrowIfNull(type);

        if (!type.IsGenericType)
        {
            return type.Name;
        }

        var genericTypeName = type.Name[..type.Name.IndexOf('`')];
        var genericArgs = type.GetGenericArguments();

        if (type.ContainsGenericParameters)
        {
            return $"{genericTypeName}<>";
        }

        var formattedArgs = string.Join(", ", genericArgs.Select(GetFriendlyName));

        return $"{genericTypeName}<{formattedArgs}>";
    }
}
