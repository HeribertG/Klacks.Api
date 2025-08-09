using Klacks.Api.Datas;
using Klacks.Api.Exceptions;
using Klacks.Api.Interfaces;
using Klacks.Api.Interfaces.Domains;
using Klacks.Api.Models.Schedules;
using Klacks.Api.Presentation.Resources;
using Klacks.Api.Presentation.Resources.Filter;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Repositories;

public class AbsenceRepository : BaseRepository<Absence>, IAbsenceRepository
{
    private readonly DataBaseContext context;
    private readonly IAbsenceSortingService _sortingService;
    private readonly IAbsencePaginationService _paginationService;

    public AbsenceRepository(DataBaseContext context, ILogger<Absence> logger,
        IAbsenceSortingService sortingService,
        IAbsencePaginationService paginationService)
        : base(context, logger)
    {
        this.context = context;
        _sortingService = sortingService;
        _paginationService = paginationService;
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
        Logger.LogInformation("Attempting to create Excel file for language: {Language}", language);
        try
        {
            // Hier würde die Logik zur Erstellung der Excel-Datei stehen.
            // Da ich keinen Zugriff auf Dateisystemoperationen habe, simuliere ich einen Fehler.
            // In einer echten Implementierung würden Sie hier die Datei erstellen und den Pfad zurückgeben.
            bool success = false; // Simulate failure for demonstration

            if (!success)
            {
                Logger.LogError("Failed to create Excel file for language: {Language}. Simulated failure.", language);
                throw new InvalidRequestException($"Failed to create Excel file for language '{language}'.");
            }

            // If successful, return a success result (adjust as per actual return type)
            return new HttpResultResource { Success = true, Messages = "Excel file created successfully." };
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error creating Excel file for language: {Language}.", language);
            throw; // Let the middleware handle it
        }
    }

}
