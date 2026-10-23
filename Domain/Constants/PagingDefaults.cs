// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Default list and search sizes: how many rows a list filter loads when the caller states no row count, the page
/// size of the generic pagination, the items per page of the criteria objects and how many hits a search returns.
/// </summary>

namespace Klacks.Api.Domain.Constants;

public static class PagingDefaults
{
    public const int DefaultRowCount = 200;

    public const int ShiftScheduleRowCount = 100;

    public const int DefaultPageSize = 20;

    public const int DefaultItemsPerPage = 10;

    public const int DefaultSearchResultLimit = 10;
}
