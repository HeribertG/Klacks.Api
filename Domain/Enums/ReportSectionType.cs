// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Domain.Enums;

/// <summary>
/// Section kinds of a report template.
/// The numbers must match the frontend enum, because stored templates carry the raw value
/// and the mapper only casts it. The previous naming here disagreed with the stored data.
/// </summary>
public enum ReportSectionType
{
    Header = 0,
    WorkTable = 1,
    ExpensesTable = 2,
    Footer = 3,
    GroupHeader = 4,
    GroupFooter = 5,
    PageHeader = 6,
    PageFooter = 7
}
