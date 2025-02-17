// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using Microsoft.Extensions.Options;
using RemoteMaster.Server.Options;

namespace RemoteMaster.Server.Validators;

[OptionsValidator]
public partial class JwtOptionsValidator : IValidateOptions<JwtOptions>;
