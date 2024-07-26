// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;

namespace RemoteMaster.Host.Windows.Tests;

public class TestableMockFileSystem(IFile file, IDictionary<string, MockFileData> files) : MockFileSystem(files)
{
    public override IFile File { get; } = file;
}