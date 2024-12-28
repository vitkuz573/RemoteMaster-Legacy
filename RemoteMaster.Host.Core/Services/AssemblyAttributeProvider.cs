// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Reflection;
using RemoteMaster.Host.Core.Abstractions;

namespace RemoteMaster.Host.Core.Services;

public class AssemblyAttributeProvider : IAssemblyAttributeProvider
{
    public T? GetCustomAttribute<T>(Assembly assembly) where T : Attribute
    {
        return assembly.GetCustomAttribute<T>();
    }
}
