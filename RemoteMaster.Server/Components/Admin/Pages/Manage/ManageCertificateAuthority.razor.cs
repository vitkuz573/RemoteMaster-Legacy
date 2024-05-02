// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using Microsoft.AspNetCore.Components;
using RemoteMaster.Server.Models;

namespace RemoteMaster.Server.Components.Admin.Pages.Manage;

public partial class ManageCertificateAuthority
{
    private string _commonName;
    private int _keySize;
    private int _validityPeriod;

    [SupplyParameterFromForm]
    private CertificateOptions CertificateDetails { get; set; } = new();

    protected override void OnInitialized()
    {
        var certificateAuthorityCertificate = CertificateAuthorityCertificate.Value;

        _commonName = certificateAuthorityCertificate.CommonName;
        _keySize = certificateAuthorityCertificate.KeySize;
        _validityPeriod = certificateAuthorityCertificate.ValidityPeriod;
    }
}
