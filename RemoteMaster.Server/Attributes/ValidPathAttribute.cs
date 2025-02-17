// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.ComponentModel.DataAnnotations;

namespace RemoteMaster.Server.Attributes;

public class ValidPathAttribute : ValidationAttribute
{
    public override bool IsValid(object? value)
    {
        if (value is string path && !string.IsNullOrWhiteSpace(path))
        {
            try
            {
                var fullPath = Path.GetFullPath(path);

                return Path.IsPathRooted(fullPath);
            }
            catch
            {
                return false;
            }
        }

        return false;
    }
}
