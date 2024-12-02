// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using RemoteMaster.Host.Core.Abstractions;

namespace RemoteMaster.Host.Core.ParameterSerializers;

public abstract class BaseParameterSerializer<T> : IParameterSerializer
{
    public bool CanHandle(ILaunchParameter parameter)
    {
        ArgumentNullException.ThrowIfNull(parameter);

        return parameter is ILaunchParameter<T>;
    }

    public void Deserialize(string? value, ILaunchParameter parameter)
    {
        ArgumentNullException.ThrowIfNull(parameter);

        if (parameter is not ILaunchParameter<T> typedParameter)
        {
            throw new ArgumentException($"Invalid parameter type for {parameter.Name}", nameof(parameter));
        }

        SetValue(typedParameter, value);
    }

    public string? Serialize(ILaunchParameter parameter, string name)
    {
        if (parameter is not ILaunchParameter<T> typedParameter)
        {
            throw new ArgumentException($"Invalid parameter type for {name}", nameof(parameter));
        }

        return GetSerializedValue(typedParameter, name);
    }

    protected abstract void SetValue(ILaunchParameter<T> parameter, string? value);

    protected abstract string? GetSerializedValue(ILaunchParameter<T> parameter, string name);
}
