// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Valid value ranges of the DATEV header fields consultant number (Beraternummer, 1001-9999999) and
/// client number (Mandantennummer, 1-99999), shared by the DATEV export formatter and the settings validation.
/// </summary>

namespace Klacks.Api.Domain.Constants;

public static class DatevNumberRanges
{
    public const int ConsultantNumberMin = 1001;
    public const int ConsultantNumberMax = 9999999;
    public const int ClientNumberMin = 1;
    public const int ClientNumberMax = 99999;
}
