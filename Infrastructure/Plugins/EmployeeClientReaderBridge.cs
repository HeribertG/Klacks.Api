// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Bridges the Contracts IEmployeeClientReader to the Core Client table.
/// Returns only non-deleted clients of EntityType Employee.
/// </summary>
/// <param name="context">EF Core database context.</param>

using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Models.Staffs;
using Klacks.Api.Infrastructure.Persistence;
using Klacks.Plugin.Contracts;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Infrastructure.Plugins;

public class EmployeeClientReaderBridge : IEmployeeClientReader
{
    private readonly DataBaseContext _context;

    public EmployeeClientReaderBridge(DataBaseContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<EmployeeClientInfo>> GetAllEmployeesAsync(CancellationToken ct = default)
    {
        var rows = await _context.Set<Client>()
            .AsNoTracking()
            .Where(c => !c.IsDeleted && c.Type == EntityTypeEnum.Employee)
            .Select(c => new { c.Id, c.FirstName })
            .ToListAsync(ct);

        return rows.Select(r => new EmployeeClientInfo(r.Id, r.FirstName)).ToList();
    }

    public async Task<EmployeeClientInfo?> GetEmployeeAsync(Guid clientId, CancellationToken ct = default)
    {
        var row = await _context.Set<Client>()
            .AsNoTracking()
            .Where(c => c.Id == clientId && !c.IsDeleted && c.Type == EntityTypeEnum.Employee)
            .Select(c => new { c.Id, c.FirstName })
            .FirstOrDefaultAsync(ct);

        return row == null ? null : new EmployeeClientInfo(row.Id, row.FirstName);
    }
}
