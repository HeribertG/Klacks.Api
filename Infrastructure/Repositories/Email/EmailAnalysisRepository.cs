// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Interfaces.Email;
using Klacks.Api.Domain.Models.Email;
using Klacks.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Infrastructure.Repositories.Email;

public class EmailAnalysisRepository : IEmailAnalysisRepository
{
    private readonly DataBaseContext _context;

    public EmailAnalysisRepository(DataBaseContext context)
    {
        _context = context;
    }

    public async Task AddAsync(EmailAnalysis analysis, CancellationToken cancellationToken = default)
    {
        await _context.EmailAnalyses.AddAsync(analysis, cancellationToken);
    }

    public async Task<EmailAnalysis?> GetByReceivedEmailIdAsync(Guid receivedEmailId, CancellationToken cancellationToken = default)
    {
        return await _context.EmailAnalyses
            .FirstOrDefaultAsync(a => a.ReceivedEmailId == receivedEmailId, cancellationToken);
    }
}
