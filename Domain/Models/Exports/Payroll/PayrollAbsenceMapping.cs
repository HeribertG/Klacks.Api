// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Domain.Models.Exports.Payroll;

public class PayrollAbsenceMapping
{
    public string Ausfallschluessel { get; set; } = string.Empty;

    public string WageType { get; set; } = string.Empty;
}
