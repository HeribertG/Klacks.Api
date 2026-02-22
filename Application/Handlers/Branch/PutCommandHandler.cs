using Klacks.Api.Application.Commands.Settings.Branch;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Infrastructure.Mediator;
using Klacks.Api.Domain.Interfaces;

namespace Klacks.Api.Application.Handlers.Branch
{
    public class PutCommandHandler : BaseHandler, IRequestHandler<PutCommand, Domain.Models.Settings.Branch?>
    {
        private readonly IBranchRepository _branchRepository;
        private readonly IUnitOfWork _unitOfWork;

        public PutCommandHandler(
            IBranchRepository branchRepository,
            IUnitOfWork unitOfWork,
            ILogger<PutCommandHandler> logger)
            : base(logger)
        {
            _branchRepository = branchRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task<Domain.Models.Settings.Branch?> Handle(PutCommand request, CancellationToken cancellationToken)
        {
            var result = await _branchRepository.Put(request.model);
            await _unitOfWork.CompleteAsync();
            return result;
        }
    }
}
