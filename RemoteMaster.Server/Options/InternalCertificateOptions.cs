// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

namespace RemoteMaster.Server.Options;

public class InternalCertificateOptions
{
    public int KeySize { get; set; }
    
    public int ValidityPeriod { get; set; }
    
    public string CommonName { get; set; }

    public SubjectOptions Subject { get; set; }
}
