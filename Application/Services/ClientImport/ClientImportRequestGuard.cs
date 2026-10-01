// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Re-checks a preview/commit request on the server, because the grid comes back from the browser:
/// a token from Parse, row, column, cell-length and total-character limits, a mapping that points at
/// existing columns with every target at most once, communication types that fit their channel, at most
/// one override per existing row with a person gender, and a well-formed policy entry date. Anything
/// else is rejected with a ClientImportErrorCodes code.
/// </summary>

using System.Globalization;
using Klacks.Api.Application.DTOs.ClientImport;
using Klacks.Api.Application.Exceptions;
using Klacks.Api.Application.Validation.Clients;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Enums;

namespace Klacks.Api.Application.Services.ClientImport;

public static class ClientImportRequestGuard
{
    private static readonly CommunicationTypeEnum[] EmailTypes = [CommunicationTypeEnum.PrivateMail, CommunicationTypeEnum.OfficeMail];
    private static readonly CommunicationTypeEnum[] PhoneTypes = [CommunicationTypeEnum.PrivateFixPhone, CommunicationTypeEnum.OfficeFixPhone];
    private static readonly CommunicationTypeEnum[] MobileTypes = [CommunicationTypeEnum.PrivateCellPhone, CommunicationTypeEnum.OfficeCellPhone];

    public static void EnsureValid(ClientImportRequest request)
    {
        request.Mapping ??= [];
        request.RowOverrides ??= [];

        if (request.Token == Guid.Empty)
        {
            throw Reject(ClientImportErrorCodes.InvalidRequest, "The import token from Parse is missing.");
        }

        EnsureGrid(request);
        EnsureMapping(request);
        EnsurePolicy(request.Policy);
        EnsureOverrides(request);
    }

    public static DateTime? ParsePolicyEntryDate(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        if (!DateTime.TryParseExact(value.Trim(), ClientImportDateParser.IsoFormat, CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var date))
        {
            throw Reject(ClientImportErrorCodes.InvalidPolicy, $"The policy entry date '{value}' is not yyyy-MM-dd.");
        }

        return date;
    }

    private static void EnsureGrid(ClientImportRequest request)
    {
        if (request.Rows == null || request.Columns == null || request.Rows.Count == 0)
        {
            throw Reject(ClientImportErrorCodes.FileEmpty, "The import contains no rows.");
        }

        if (request.Rows.Count > ClientImportLimits.MaxRows)
        {
            throw Reject(ClientImportErrorCodes.TooManyRows, $"The import has {request.Rows.Count} rows, more than {ClientImportLimits.MaxRows}.");
        }

        if (request.Columns.Count > ClientImportLimits.MaxColumns)
        {
            throw Reject(ClientImportErrorCodes.TooManyColumns, $"The import has {request.Columns.Count} columns, more than {ClientImportLimits.MaxColumns}.");
        }

        foreach (var row in request.Rows)
        {
            if (row == null || row.Count > ClientImportLimits.MaxColumns
                || row.Any(cell => cell != null && cell.Length > ClientImportLimits.MaxCellLength))
            {
                throw Reject(ClientImportErrorCodes.InvalidRequest, "A row is missing, too wide or has a cell above the length limit.");
            }
        }

        if (ClientImportGridBudget.Exceeds(request.Rows))
        {
            throw Reject(ClientImportErrorCodes.InvalidRequest, $"The import holds more than {ClientImportLimits.MaxGridChars} characters.");
        }
    }

    private static void EnsureMapping(ClientImportRequest request)
    {
        var columnCount = Math.Max(request.Columns.Count, request.Rows.Max(r => r.Count));
        var mapping = request.Mapping;

        if (mapping.Any(m => m.ColumnIndex < 0 || m.ColumnIndex >= columnCount || !Enum.IsDefined(m.Target)))
        {
            throw Reject(ClientImportErrorCodes.InvalidRequest, "The mapping points at a column that does not exist.");
        }

        if (mapping.GroupBy(m => m.ColumnIndex).Any(g => g.Count() > 1)
            || mapping.Where(m => m.Target != ClientImportTarget.Ignore).GroupBy(m => m.Target).Any(g => g.Count() > 1))
        {
            throw Reject(ClientImportErrorCodes.InvalidRequest, "Every column and every target may be mapped only once.");
        }
    }

    private static void EnsurePolicy(ClientImportPolicy? policy)
    {
        if (policy == null
            || !EmailTypes.Contains(policy.EmailType)
            || !PhoneTypes.Contains(policy.PhoneType)
            || !MobileTypes.Contains(policy.MobileType)
            || !Enum.IsDefined(policy.FormerEmployees)
            || !Enum.IsDefined(policy.Duplicates))
        {
            throw Reject(ClientImportErrorCodes.InvalidPolicy, "The import policy is missing or has an unsupported value.");
        }

        ParsePolicyEntryDate(policy.EntryDate);
    }

    private static void EnsureOverrides(ClientImportRequest request)
    {
        if (request.RowOverrides.Count > request.Rows.Count
            || request.RowOverrides.Any(o => o == null)
            || request.RowOverrides.GroupBy(o => o.RowIndex).Any(g => g.Count() > 1))
        {
            throw Reject(ClientImportErrorCodes.InvalidRequest, "There may be at most one override per row.");
        }

        foreach (var rowOverride in request.RowOverrides)
        {
            if (rowOverride.RowIndex < 0 || rowOverride.RowIndex >= request.Rows.Count
                || (rowOverride.Gender is { } gender && !ClientPersonRules.IsPersonGender(gender)))
            {
                throw Reject(ClientImportErrorCodes.InvalidRequest, "A row override points at a missing row or has an unsupported gender.");
            }
        }
    }

    private static ClientImportRejectedException Reject(string code, string message) => new(code, message);
}
