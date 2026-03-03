// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.DTOs.Email;
using Klacks.Api.Application.Mappers;
using Klacks.Api.Application.Queries.Email;
using Klacks.Api.Domain.Interfaces.Email;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.Email;

public class GetReceivedEmailsQueryHandler : BaseHandler, IRequestHandler<GetReceivedEmailsQuery, ReceivedEmailListResponse>
{
    private readonly IReceivedEmailRepository _repository;
    private readonly ReceivedEmailMapper _mapper;

    public GetReceivedEmailsQueryHandler(
        IReceivedEmailRepository repository,
        ReceivedEmailMapper mapper,
        ILogger<GetReceivedEmailsQueryHandler> logger)
        : base(logger)
    {
        _repository = repository;
        _mapper = mapper;
    }

    public async Task<ReceivedEmailListResponse> Handle(GetReceivedEmailsQuery request, CancellationToken cancellationToken)
    {
        return await ExecuteAsync(async () =>
        {
            bool? isRead = request.ReadFilter?.ToLowerInvariant() switch
            {
                "read" => true,
                "unread" => false,
                _ => null
            };

            var sortAsc = string.Equals(request.SortDirection, "asc", StringComparison.OrdinalIgnoreCase);

            var emails = await _repository.GetFilteredListAsync(request.Folder, isRead, sortAsc, request.Skip, request.Take);
            var totalCount = await _repository.GetFilteredCountAsync(request.Folder, isRead);
            var unreadCount = await _repository.GetFilteredCountAsync(request.Folder, false);

            return new ReceivedEmailListResponse
            {
                Items = _mapper.ToListResources(emails),
                TotalCount = totalCount,
                UnreadCount = unreadCount
            };
        }, nameof(GetReceivedEmailsQuery));
    }
}
