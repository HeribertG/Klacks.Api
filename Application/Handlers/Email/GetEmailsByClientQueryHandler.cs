// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.DTOs.Email;
using Klacks.Api.Application.Mappers;
using Klacks.Api.Application.Queries.Email;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Infrastructure.Mediator;
using Klacks.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Application.Handlers.Email;

public class GetEmailsByClientQueryHandler : BaseHandler, IRequestHandler<GetEmailsByClientQuery, ReceivedEmailListResponse>
{
    private readonly DataBaseContext _context;
    private readonly ReceivedEmailMapper _mapper;

    public GetEmailsByClientQueryHandler(
        DataBaseContext context,
        ReceivedEmailMapper mapper,
        ILogger<GetEmailsByClientQueryHandler> logger)
        : base(logger)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<ReceivedEmailListResponse> Handle(GetEmailsByClientQuery request, CancellationToken cancellationToken)
    {
        return await ExecuteAsync(async () =>
        {
            var emailAddresses = await _context.Set<Domain.Models.Staffs.Communication>()
                .Where(c => !c.IsDeleted &&
                            c.ClientId == request.ClientId &&
                            (c.Type == CommunicationTypeEnum.PrivateMail || c.Type == CommunicationTypeEnum.OfficeMail) &&
                            !string.IsNullOrEmpty(c.Value))
                .Select(c => c.Value.ToLower())
                .Distinct()
                .ToListAsync(cancellationToken);

            if (emailAddresses.Count == 0)
                return new ReceivedEmailListResponse { Items = [], TotalCount = 0, UnreadCount = 0 };

            var query = _context.ReceivedEmails
                .Where(e => !e.IsDeleted &&
                            e.Folder == EmailConstants.ClientAssignedFolder &&
                            emailAddresses.Contains(e.FromAddress.ToLower()));

            var totalCount = await query.CountAsync(cancellationToken);
            var unreadCount = await query.CountAsync(e => !e.IsRead, cancellationToken);

            var emails = await query
                .OrderByDescending(e => e.ReceivedDate)
                .Skip(request.Skip)
                .Take(request.Take)
                .ToListAsync(cancellationToken);

            return new ReceivedEmailListResponse
            {
                Items = _mapper.ToListResources(emails),
                TotalCount = totalCount,
                UnreadCount = unreadCount
            };
        }, nameof(GetEmailsByClientQuery));
    }
}
