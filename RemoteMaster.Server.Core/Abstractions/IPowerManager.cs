// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// Unauthorized copying of this file, via any medium is strictly prohibited.

namespace RemoteMaster.Server.Core.Abstractions;

public interface IPowerManager
{
    void Shutdown();

    void Reboot();
}
