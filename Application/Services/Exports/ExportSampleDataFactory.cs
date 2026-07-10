// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Deterministic sample datasets for export formatters, shared by the override preview endpoint and
/// the golden regression tests. Every value (ids, dates, names with umlauts, quantities) is fixed so
/// formatter output is byte-stable across runs; changing this factory invalidates all golden files.
/// </summary>
using Klacks.Api.Application.Constants;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Models.Exports;
using Klacks.Api.Domain.Models.Exports.Payroll;

namespace Klacks.Api.Application.Services.Exports;

public static class ExportSampleDataFactory
{
    public static readonly Guid SampleGroupId = Guid.Parse("a1000000-0000-4000-8000-000000000001");
    public static readonly Guid SampleAbsenceId = Guid.Parse("a2000000-0000-4000-8000-000000000002");

    private static readonly Guid OrderShiftId = Guid.Parse("a3000000-0000-4000-8000-000000000003");
    private static readonly Guid CustomerId = Guid.Parse("a4000000-0000-4000-8000-000000000004");
    private static readonly Guid EmployeeOneId = Guid.Parse("a5000000-0000-4000-8000-000000000005");
    private static readonly Guid EmployeeTwoId = Guid.Parse("a6000000-0000-4000-8000-000000000006");
    private static readonly Guid WorkOneId = Guid.Parse("a7000000-0000-4000-8000-000000000007");
    private static readonly Guid WorkTwoId = Guid.Parse("a8000000-0000-4000-8000-000000000008");

    private static readonly DateOnly PeriodStart = new(2026, 1, 1);
    private static readonly DateOnly PeriodEnd = new(2026, 1, 31);
    private static readonly DateTime ExportStamp = new(2026, 2, 1, 8, 0, 0, DateTimeKind.Utc);

    public static OrderExportData CreateOrderSample()
    {
        return new OrderExportData
        {
            StartDate = PeriodStart,
            EndDate = PeriodEnd,
            ExportDate = ExportStamp,
            Orders =
            [
                new OrderGroup
                {
                    OrderShiftId = OrderShiftId,
                    OrderName = "Musterauftrag Zürich Süd",
                    OrderAbbreviation = "MZS",
                    SourceSystemId = "ERP-01",
                    ExternalOrderReference = "PO-2026-0042",
                    OrderFromDate = PeriodStart,
                    OrderUntilDate = PeriodEnd,
                    OrderStartShift = new TimeOnly(6, 0),
                    OrderEndShift = new TimeOnly(22, 0),
                    CustomerId = CustomerId,
                    CustomerNumber = 10042,
                    CustomerName = "Müller & Söhne AG",
                    CustomerExternalReference = "CUST-042",
                    WorkEntries =
                    [
                        new WorkExportEntry
                        {
                            WorkId = WorkOneId,
                            EmployeeId = EmployeeOneId,
                            EmployeeName = "Æbelø, François",
                            EmployeeIdNumber = 42,
                            WorkDate = new DateOnly(2026, 1, 15),
                            StartTime = new TimeOnly(6, 0),
                            EndTime = new TimeOnly(14, 30),
                            WorkTime = 8.5m,
                            Surcharges = 1.25m,
                            Information = "Frühdienst",
                        },
                        new WorkExportEntry
                        {
                            WorkId = WorkTwoId,
                            EmployeeId = EmployeeTwoId,
                            EmployeeName = "Öhlund, Björn",
                            EmployeeIdNumber = 77,
                            WorkDate = new DateOnly(2026, 1, 16),
                            StartTime = new TimeOnly(14, 0),
                            EndTime = new TimeOnly(22, 0),
                            WorkTime = 8m,
                            Surcharges = 0m,
                        },
                    ],
                },
            ],
        };
    }

    public static PayrollExportData CreatePayrollSample()
    {
        return new PayrollExportData
        {
            GroupId = SampleGroupId,
            StartDate = PeriodStart,
            EndDate = PeriodEnd,
            ExportDate = ExportStamp,
            Employees =
            [
                new PayrollEmployee
                {
                    ClientId = EmployeeOneId,
                    IdNumber = 42,
                    FullName = "Æbelø, François",
                    Entries =
                    [
                        new PayrollDayEntry { Date = new DateOnly(2026, 1, 15), Kind = PayrollEntryKind.WorkHours, Quantity = 8.5m },
                        new PayrollDayEntry { Date = new DateOnly(2026, 1, 15), Kind = PayrollEntryKind.Surcharge, Quantity = 1.25m },
                        new PayrollDayEntry { Date = new DateOnly(2026, 1, 20), Kind = PayrollEntryKind.Absence, Quantity = 8m, AbsenceId = SampleAbsenceId },
                    ],
                },
                new PayrollEmployee
                {
                    ClientId = EmployeeTwoId,
                    IdNumber = 77,
                    FullName = "Öhlund, Björn",
                    Entries =
                    [
                        new PayrollDayEntry { Date = new DateOnly(2026, 1, 16), Kind = PayrollEntryKind.WorkHours, Quantity = 8m },
                    ],
                },
            ],
        };
    }

    public static PayrollExportGroupConfig CreatePayrollSampleConfig(string targetSystem)
    {
        return new PayrollExportGroupConfig
        {
            GroupId = SampleGroupId,
            TargetSystem = targetSystem,
            Delimiter = PayrollExportConstants.DefaultDelimiter,
            Encoding = PayrollExportConstants.DefaultEncoding,
            BaseWageType = "1000",
            SurchargeWageType = "1010",
            AbsenceMappingJson = "{\"" + SampleAbsenceId + "\":{\"WageType\":\"2000\",\"Ausfallschluessel\":\"K\"}}",
        };
    }

    public static ClientPeriodExportData CreateClientPeriodSample()
    {
        return new ClientPeriodExportData
        {
            StartDate = PeriodStart,
            EndDate = PeriodEnd,
            ExportDate = ExportStamp,
            Clients =
            [
                new ClientPeriodGroup
                {
                    ClientId = EmployeeOneId,
                    ClientName = "Æbelø, François",
                    ClientIdNumber = 42,
                    ClientType = EntityTypeEnum.Employee,
                    WorkEntries =
                    [
                        new ClientWorkExportEntry
                        {
                            WorkId = WorkOneId,
                            WorkDate = new DateOnly(2026, 1, 15),
                            StartTime = new TimeOnly(6, 0),
                            EndTime = new TimeOnly(14, 30),
                            WorkTime = 8.5m,
                            Surcharges = 1.25m,
                            Information = "Frühdienst",
                        },
                    ],
                },
            ],
        };
    }

    public static ExportOptions CreateSampleOptions()
    {
        return new ExportOptions
        {
            Language = "de",
            CurrencyCode = "EUR",
            Company = new CompanyInfo
            {
                Name = "Klacks Muster GmbH",
                TaxId = "99/999/99999",
                VatId = "DE999999999",
                CommercialRegister = "HRB 99999",
            },
        };
    }
}
