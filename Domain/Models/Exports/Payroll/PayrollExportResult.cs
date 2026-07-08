// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Domain.Models.Exports.Payroll;

public class PayrollExportResult
{
    public byte[] Content { get; set; } = [];

    public int RecordCount { get; set; }

    public int SkippedAbsenceCount { get; set; }
}
