// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Domain.Models.Exports.Payroll;

public class PayrollExportResult
{
    public byte[] Content { get; set; } = [];

    public int RecordCount { get; set; }

    public int SkippedAbsenceCount { get; set; }
}
