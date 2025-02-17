// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.ComponentModel.DataAnnotations;

namespace RemoteMaster.Server.Attributes;

public class CustomMinLengthAttribute(int length) : ValidationAttribute
{
    public int Length { get; } = length;

    public override bool IsValid(object? value)
    {
        if (value is string str)
        {
            return str.Length >= Length;
        }

        return false;
    }
}
