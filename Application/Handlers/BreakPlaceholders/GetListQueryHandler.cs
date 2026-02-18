using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Mappers;
using Klacks.Api.Application.Queries.BreakPlaceholders;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Exceptions;
using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.BreakPlaceholders;

public class GetListQueryHandler : IRequestHandler<ListQuery, (IEnumerable<ClientBreakPlaceholderResource> Clients, int TotalCount)>
{
    private readonly IClientBreakPlaceholderRepository _clientBreakPlaceholderRepository;
    private readonly ScheduleMapper _scheduleMapper;
    private readonly FilterMapper _filterMapper;
    private readonly ClientMapper _clientMapper;
    private readonly ILogger<GetListQueryHandler> _logger;

    public GetListQueryHandler(
        IClientBreakPlaceholderRepository clientBreakPlaceholderRepository,
        ScheduleMapper scheduleMapper,
        FilterMapper filterMapper,
        ClientMapper clientMapper,
        ILogger<GetListQueryHandler> logger)
    {
        _clientBreakPlaceholderRepository = clientBreakPlaceholderRepository;
        _scheduleMapper = scheduleMapper;
        _filterMapper = filterMapper;
        _clientMapper = clientMapper;
        _logger = logger;
    }

    public async Task<(IEnumerable<ClientBreakPlaceholderResource> Clients, int TotalCount)> Handle(ListQuery request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Fetching client break list with filter");

            if (request.Filter == null)
            {
                _logger.LogWarning("Break filter is null");
                throw new InvalidRequestException("Filter parameter is required for break list query");
            }

            var breakFilter = _filterMapper.ToBreakFilter(request.Filter);

            var (clients, totalCount) = await _clientBreakPlaceholderRepository.BreakList(breakFilter);
            var clientList = clients.ToList();

            _logger.LogInformation("Retrieved {Count} clients with break data (Total: {TotalCount})", clientList.Count, totalCount);

            var mappedClients = clientList.Select(c =>
            {
                var resource = _clientMapper.ToBreakPlaceholderResource(c);
                resource.BreakPlaceholders = resource.BreakPlaceholders
                    .Concat(MergeScheduleBreaks(c.Breaks))
                    .ToList();
                return resource;
            }).ToList();
            return (mappedClients, totalCount);
        }
        catch (InvalidRequestException)
        {
            throw;
        }
        catch (FormatException ex)
        {
            _logger.LogError(ex, "Invalid format in break filter parameters");
            throw new InvalidRequestException("Invalid format in filter parameters");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while fetching client break list");
            throw new InvalidRequestException($"Failed to retrieve client break list: {ex.Message}");
        }
    }

    private static List<BreakPlaceholderResource> MergeScheduleBreaks(ICollection<Break>? breaks)
    {
        if (breaks == null || breaks.Count == 0)
            return [];

        var result = new List<BreakPlaceholderResource>();

        foreach (var group in breaks.GroupBy(b => b.AbsenceId))
        {
            var sorted = group.OrderBy(b => b.CurrentDate).ToList();
            var rangeStart = sorted[0];
            var rangeEnd = sorted[0];

            for (var i = 1; i < sorted.Count; i++)
            {
                var current = sorted[i];
                if (current.CurrentDate == rangeEnd.CurrentDate.AddDays(1))
                {
                    rangeEnd = current;
                }
                else
                {
                    result.Add(CreateMergedResource(rangeStart, rangeEnd));
                    rangeStart = current;
                    rangeEnd = current;
                }
            }

            result.Add(CreateMergedResource(rangeStart, rangeEnd));
        }

        return result;
    }

    private static BreakPlaceholderResource CreateMergedResource(Break first, Break last)
    {
        return new BreakPlaceholderResource
        {
            Id = first.Id,
            ClientId = first.ClientId,
            AbsenceId = first.AbsenceId,
            Information = first.Information,
            EntrySource = EntrySource.Schedule,
            From = first.CurrentDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc),
            Until = last.CurrentDate.ToDateTime(new TimeOnly(23, 59, 0), DateTimeKind.Utc)
        };
    }
}
