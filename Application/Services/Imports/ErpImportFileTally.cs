// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Mutable outcome counters of one ERP import file attempt. The importer records one outcome per order
/// (parse-rejected orders included, so the processed count reaches the total at the end) and
/// snapshots the counters into an ErpImportFileProgress for every heartbeat and for the finish.
/// </summary>
/// <param name="totalOrders">Parsed orders plus the orders the parser rejected (one per rejected order element)</param>
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Models.Imports;

namespace Klacks.Api.Application.Services.Imports;

public sealed class ErpImportFileTally
{
    private readonly int _totalOrders;
    private int _processed;
    private int _created;
    private int _updated;
    private int _unchanged;
    private int _superseded;
    private int _rejected;

    public ErpImportFileTally(int totalOrders)
    {
        _totalOrders = totalOrders;
    }

    public int RejectedCount => _rejected;

    public void Record(ImportedOrderOutcome outcome)
    {
        _processed++;

        switch (outcome)
        {
            case ImportedOrderOutcome.Created:
                _created++;
                break;
            case ImportedOrderOutcome.Updated:
                _updated++;
                break;
            case ImportedOrderOutcome.Unchanged:
                _unchanged++;
                break;
            case ImportedOrderOutcome.Superseded:
                _superseded++;
                break;
            case ImportedOrderOutcome.Rejected:
                _rejected++;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(outcome), outcome, "Unknown imported order outcome.");
        }
    }

    public ErpImportFileProgress ToProgress()
    {
        return new ErpImportFileProgress(_totalOrders, _processed, _created, _updated, _unchanged, _superseded, _rejected);
    }
}
