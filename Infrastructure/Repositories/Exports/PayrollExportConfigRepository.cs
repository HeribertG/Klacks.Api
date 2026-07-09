// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Reads the per-group payroll-export configuration from the database, falling back to a sensible
/// DATEV Lohn &amp; Gehalt default when no row exists for the group.
/// </summary>
using Klacks.Api.Application.Constants;
using Klacks.Api.Application.Interfaces.Exports;
using Klacks.Api.Domain.Models.Exports.Payroll;
using Klacks.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Infrastructure.Repositories.Exports;

public class PayrollExportConfigRepository : IPayrollExportConfigRepository
{
    private readonly DataBaseContext _context;

    public PayrollExportConfigRepository(DataBaseContext context)
    {
        _context = context;
    }

    public async Task<PayrollExportGroupConfig> GetByGroupAsync(Guid groupId, CancellationToken cancellationToken = default)
    {
        var config = await _context.PayrollExportGroupConfig
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.GroupId == groupId, cancellationToken);

        return config ?? CreateDefault(groupId);
    }

    private static PayrollExportGroupConfig CreateDefault(Guid groupId)
    {
        return new PayrollExportGroupConfig
        {
            GroupId = groupId,
            TargetSystem = PayrollExportConstants.FormatKeyDatevLug,
            Delimiter = PayrollExportConstants.DefaultDelimiter,
            Encoding = PayrollExportConstants.DefaultEncoding,
            BaseWageType = string.Empty,
            SurchargeWageType = string.Empty,
            AbsenceMappingJson = "{}",
        };
    }
}
