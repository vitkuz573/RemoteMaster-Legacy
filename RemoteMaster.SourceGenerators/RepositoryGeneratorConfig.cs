// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

namespace RemoteMaster.SourceGenerators;

internal class RepositoryGeneratorConfig
{
    public bool GenerateRepositoriesInSeparateFiles { get; set; }

    public bool GenerateUnitOfWorkInSeparateFiles { get; set; }

    public string? GeneratedNamespace { get; set; }
}
