// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Globalization;
using Avalonia.Data.Converters;

namespace RemoteMaster.Host.Chat.Avalonia.Converters;

public class CurrentUserVisibilityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var messageUser = value as string;
        var currentUser = parameter as string;

        return messageUser == currentUser;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}