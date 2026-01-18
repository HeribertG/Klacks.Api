using Klacks.Api.Infrastructure.Persistence;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Presentation.DTOs;
using Klacks.Api.Presentation.DTOs.Filter;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Infrastructure.Repositories;

public class AbsenceRepository : BaseRepository<Absence>, IAbsenceRepository
{
    private readonly DataBaseContext context;
    private readonly IAbsenceSortingService _sortingService;
    private readonly IAbsencePaginationService _paginationService;
    private readonly IAbsenceExportService _exportService;

    public AbsenceRepository(DataBaseContext context, ILogger<Absence> logger,
        IAbsenceSortingService sortingService,
        IAbsencePaginationService paginationService,
        IAbsenceExportService exportService)
        : base(context, logger)
    {
        this.context = context;
        _sortingService = sortingService;
        _paginationService = paginationService;
        _exportService = exportService;
    }

    public override async Task<Absence?> Delete(Guid id)
    {
        var absence = await context.Absence.FirstOrDefaultAsync(a => a.Id == id);
        if (absence == null)
        {
            return null;
        }

        var absenceDetails = await context.AbsenceDetail
            .Where(ad => ad.AbsenceId == id && !ad.IsDeleted)
            .ToListAsync();

        foreach (var absenceDetail in absenceDetails)
        {
            context.Remove(absenceDetail);
        }

        context.Remove(absence);
        return absence;
    }

    public async Task<TruncatedAbsence> Truncated(AbsenceFilter filter)
    {
        Logger.LogInformation("Getting truncated absences with filter");
        
        var query = context.Absence.AsQueryable();
        
        query = _sortingService.ApplySorting(query, filter.OrderBy, filter.SortOrder, filter.Language);
        
        var result = await _paginationService.ApplyPaginationAsync(query, filter);
        
        Logger.LogInformation("Retrieved {Count} absences out of {Total}", 
            result.Absences.Count, result.MaxItems);
        
        return result;
    }

    public HttpResultResource CreateExcelFile(string language)
    {
        return _exportService.CreateExcelFile(language);
    }
}
