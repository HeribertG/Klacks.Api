using Klacks.Api.Domain.Common;
using Klacks.Api.Infrastructure.Persistence;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Presentation.DTOs;
using Klacks.Api.Presentation.DTOs.Filter;

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
    
    public async Task<TruncatedAbsence> Truncated(AbsenceFilter filter)
    {
        Logger.LogInformation("Getting truncated absences with filter");
        
        var query = context.Absence.AsQueryable();
        
        // Apply sorting using domain service
        query = _sortingService.ApplySorting(query, filter.OrderBy, filter.SortOrder, filter.Language);
        
        // Apply pagination using domain service
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
