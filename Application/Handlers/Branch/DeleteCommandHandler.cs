using AutoMapper;
using Klacks.Api.Application.Commands.Settings.Branch;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Application.Handlers.Branch;

public class DeleteCommandHandler : BaseHandler, IRequestHandler<DeleteCommand>
{
    private readonly IBranchRepository _branchRepository;
    private readonly DataBaseContext _context;

    public DeleteCommandHandler(
        IBranchRepository branchRepository,
        DataBaseContext context,
        ILogger<DeleteCommandHandler> logger)
        : base(logger)
    {
        _branchRepository = branchRepository;
        _context = context;
    }

    public async Task Handle(DeleteCommand request, CancellationToken cancellationToken)
    {
        await _context.Branch.Where(b => b.Id == request.id).ExecuteDeleteAsync(cancellationToken);
    }
}
