// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.Commands.Settings.Branch;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Infrastructure.Mediator;
using Klacks.Api.Domain.Interfaces;

namespace Klacks.Api.Application.Handlers.Branch
{
    public class PostCommandHandler : BaseHandler, IRequestHandler<PostCommand, Domain.Models.Settings.Branch?>
    {
        private readonly IBranchRepository _branchRepository;
        private readonly IUnitOfWork _unitOfWork;

        public PostCommandHandler(
            IBranchRepository branchRepository,
            IUnitOfWork unitOfWork,
            ILogger<PostCommandHandler> logger)
            : base(logger)
        {
            _branchRepository = branchRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task<Domain.Models.Settings.Branch?> Handle(PostCommand request, CancellationToken cancellationToken)
        {
            await _branchRepository.Add(request.model);
            await _unitOfWork.CompleteAsync();
            return request.model;
        }
    }
}
