// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Collects the seeded customers, renders the demo order import document and stores it under the
/// content root. The file is deliberately not written into the ERP drop point: the background import
/// service polls that folder every minute, so the operator must copy it there explicitly.
/// </summary>
/// <param name="context">Database context the seeded customers are read from</param>
/// <param name="generator">Renderer turning the demo order definitions into the import document</param>
/// <param name="environment">Host environment providing the content root the file is written to</param>
/// <param name="logger">Logger reporting the written path</param>

using Klacks.Api.Application.DTOs.Seed;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Infrastructure.Persistence.Seed.Demo;

public class DemoOrderSeedFileWriter : IDemoOrderSeedFileWriter
{
    private readonly DataBaseContext _context;
    private readonly IDemoOrderXmlGenerator _generator;
    private readonly IHostEnvironment _environment;
    private readonly ILogger<DemoOrderSeedFileWriter> _logger;

    public DemoOrderSeedFileWriter(
        DataBaseContext context,
        IDemoOrderXmlGenerator generator,
        IHostEnvironment environment,
        ILogger<DemoOrderSeedFileWriter> logger)
    {
        _context = context;
        _generator = generator;
        _environment = environment;
        _logger = logger;
    }

    public async Task<string?> WriteAsync(string language, CancellationToken cancellationToken = default)
    {
        var customers = await LoadCustomersAsync(cancellationToken);
        if (customers.Count == 0)
        {
            _logger.LogWarning("No seeded customer with company and address found; demo order file was not written.");
            return null;
        }

        var xml = _generator.Generate(customers, language);

        var directory = Path.Combine(_environment.ContentRootPath, DemoOrderSeedConstants.SeedDataDirectoryName);
        Directory.CreateDirectory(directory);
        var path = Path.Combine(directory, DemoOrderSeedConstants.DemoOrdersFileName);
        await File.WriteAllTextAsync(path, xml, cancellationToken);

        _logger.LogInformation(
            "Demo shifts were not seeded; {OrderCount} demo orders for {CustomerCount} customers were written to {Path}. Copy this file into the ERP drop point to import them.",
            xml.Split($"<{ErpImportXmlElements.Order}>").Length - 1,
            customers.Count,
            path);

        return path;
    }

    private async Task<List<DemoOrderCustomer>> LoadCustomersAsync(CancellationToken cancellationToken)
    {
        return await _context.Client
            .Where(c => c.Type == EntityTypeEnum.Customer && c.Company != null && c.Company != string.Empty)
            .OrderBy(c => c.IdNumber)
            .Select(c => new DemoOrderCustomer
            {
                IdNumber = c.IdNumber,
                Company = c.Company!,
                Street = c.Addresses.Where(a => !a.IsDeleted).OrderBy(a => a.ValidFrom).Select(a => a.Street).FirstOrDefault() ?? string.Empty,
                Zip = c.Addresses.Where(a => !a.IsDeleted).OrderBy(a => a.ValidFrom).Select(a => a.Zip).FirstOrDefault() ?? string.Empty,
                City = c.Addresses.Where(a => !a.IsDeleted).OrderBy(a => a.ValidFrom).Select(a => a.City).FirstOrDefault() ?? string.Empty,
                State = c.Addresses.Where(a => !a.IsDeleted).OrderBy(a => a.ValidFrom).Select(a => a.State).FirstOrDefault() ?? string.Empty,
                Country = c.Addresses.Where(a => !a.IsDeleted).OrderBy(a => a.ValidFrom).Select(a => a.Country).FirstOrDefault() ?? string.Empty
            })
            .Where(c => c.Street != string.Empty && c.Zip != string.Empty)
            .ToListAsync(cancellationToken);
    }
}
