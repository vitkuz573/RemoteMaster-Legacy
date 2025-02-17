// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.ComponentModel.DataAnnotations;
using RemoteMaster.Server.Attributes;

namespace RemoteMaster.Server.Options;

public class JwtOptions
{
    [Required(ErrorMessage = "KeysDirectory cannot be null or empty.")]
    [ValidPath(ErrorMessage = "KeysDirectory is not a valid path.")]
    public string KeysDirectory { get; set; } = string.Empty;

    [Required(ErrorMessage = "KeySize is required.")]
    [AllowedValues(2048, 4096, ErrorMessage = "KeySize must be either 2048 or 4096.")]
    public int KeySize { get; set; } = 2048;

    [Required(ErrorMessage = "KeyPassword cannot be null or empty.")]
    [CustomMinLength(8, ErrorMessage = "KeyPassword must be at least 8 characters long.")]
    [RegularExpression("^(?=.*[a-z])(?=.*[A-Z])(?=.*\\d).{8,}$", ErrorMessage = "KeyPassword must contain at least one uppercase letter, one lowercase letter, and one digit.")]
    public string KeyPassword { get; set; } = string.Empty;
}
