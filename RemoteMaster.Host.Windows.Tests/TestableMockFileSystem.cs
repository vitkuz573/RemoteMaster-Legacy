// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;

namespace RemoteMaster.Host.Windows.Tests;

public class TestableMockFileSystem : MockFileSystem
{
    private readonly IFile _file;

    public TestableMockFileSystem(IFile file)
    {
        _file = file;
    }

    public TestableMockFileSystem(IFile file, IDictionary<string, MockFileData> files) : base(files)
    {
        _file = file;
    }

    public override IFile File => _file;
}