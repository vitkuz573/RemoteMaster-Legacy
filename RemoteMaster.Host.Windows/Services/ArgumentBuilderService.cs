// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Text;
using RemoteMaster.Host.Windows.Abstractions;

namespace RemoteMaster.Host.Windows.Services;

public class ArgumentBuilderService : IArgumentBuilderService
{
    public string BuildArguments(Dictionary<string, object> arguments)
    {
        ArgumentNullException.ThrowIfNull(arguments);

        var argumentsString = new StringBuilder();

        foreach (var argument in arguments)
        {
            switch (argument.Value)
            {
                case string strValue:
                    {
                        var escapedValue = "\"" + strValue.Replace("\"", "\\\"") + "\"";
                        argumentsString.Append($" --{argument.Key}={escapedValue}");
                        break;
                    }
                case bool boolValue:
                    argumentsString.Append($" --{argument.Key}={boolValue.ToString().ToLower()}");
                    break;
                default:
                    argumentsString.Append($" --{argument.Key}={argument.Value}");
                    break;
            }
        }

        return argumentsString.ToString().Trim();
    }
}