// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

namespace RemoteMaster.Host.Core.Abstractions;

public abstract class LaunchModeBase
{
    public abstract string Name { get; }

    public abstract string Description { get; }


    private readonly List<KeyValuePair<string, ILaunchParameter>> _parameters = [];

    /// <summary>
    /// Retrieves all parameters as a read-only list while maintaining insertion order.
    /// </summary>
    public IReadOnlyList<KeyValuePair<string, ILaunchParameter>> Parameters => _parameters;

    protected LaunchModeBase()
    {
        InitializeParameters();
    }

    /// <summary>
    /// Initializes parameters for this specific launch mode.
    /// </summary>
    protected abstract void InitializeParameters();

    /// <summary>
    /// Adds a typed parameter to the collection.
    /// </summary>
    /// <typeparam name="T">The type of the parameter value.</typeparam>
    /// <param name="parameter">The parameter to add.</param>
    protected void AddParameter<T>(ILaunchParameter<T> parameter)
    {
        ArgumentNullException.ThrowIfNull(parameter);

        if (_parameters.Any(p => string.Equals(p.Key, parameter.Name, StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidOperationException($"Parameter name '{parameter.Name}' is already defined.");
        }

        _parameters.Add(new KeyValuePair<string, ILaunchParameter>(parameter.Name, parameter));

        foreach (var alias in parameter.Aliases.Where(alias => alias != parameter.Name))
        {
            if (_parameters.Any(p => string.Equals(p.Key, alias, StringComparison.OrdinalIgnoreCase)))
            {
                throw new InvalidOperationException($"Alias '{alias}' is already associated with another parameter.");
            }

            _parameters.Add(new KeyValuePair<string, ILaunchParameter>(alias, parameter));
        }
    }

    /// <summary>
    /// Retrieves a parameter by name or alias.
    /// </summary>
    /// <typeparam name="T">The expected type of the parameter.</typeparam>
    /// <param name="name">The name or alias of the parameter.</param>
    /// <returns>The parameter as <see cref="ILaunchParameter{T}"/>.</returns>
    public ILaunchParameter<T>? GetParameter<T>(string name)
    {
        var parameterEntry = _parameters.FirstOrDefault(p => string.Equals(p.Key, name, StringComparison.OrdinalIgnoreCase));

        if (parameterEntry.Value is ILaunchParameter<T> typedParameter)
        {
            return typedParameter;
        }

        return null;
    }

    /// <summary>
    /// Executes the main logic for this launch mode.
    /// </summary>
    /// <param name="serviceProvider">The service provider.</param>
    /// <param name="cancellationToken">Cancellation token for asynchronous execution.</param>
    public abstract Task ExecuteAsync(IServiceProvider serviceProvider, CancellationToken cancellationToken = default);
}
