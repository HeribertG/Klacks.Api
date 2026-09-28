// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Application.Configuration;

public class DataProtectionKeyRingOptions
{
    public const string SectionName = "DataProtection";

    public string? CertificateBase64 { get; set; }

    public string? CertificatePassword { get; set; }

    public string? CertificatePath { get; set; }
}
