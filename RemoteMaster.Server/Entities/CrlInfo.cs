// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

namespace RemoteMaster.Server.Entities;

public class CrlInfo
{
    private CrlInfo() { }

    public CrlInfo(string crlNumber)
    {
        CrlNumber = crlNumber;
    }

    public int Id { get; private set; }

    public string CrlNumber { get; private set; }

    public DateTimeOffset NextUpdate { get; private set; }

    public string CrlHash { get; private set; }

    public void SetNumber(string number)
    {
        CrlNumber = number;
    }

    public void SetNextUpdate(DateTimeOffset nextUpdate)
    {
        NextUpdate = nextUpdate;
    }

    public void SetHash(string hash)
    {
        CrlHash = hash;
    }
}
