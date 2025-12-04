using AutoMapper;
using Klacks.Api.Application.Commands.Settings.Branch;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.Branch
{
    public class PutCommandHandler : BaseHandler, IRequestHandler<PutCommand, Domain.Models.Settings.Branch?>
    {
        private readonly IBranchRepository _branchRepository;
        private readonly IMapper _mapper;
        private readonly IUnitOfWork _unitOfWork;

        public PutCommandHandler(
            IBranchRepository branchRepository,
            IMapper mapper,
            IUnitOfWork unitOfWork,
            ILogger<PutCommandHandler> logger)
            : base(logger)
        {
            _branchRepository = branchRepository;
            _mapper = mapper;
            _unitOfWork = unitOfWork;
        }

        public async Task<Domain.Models.Settings.Branch?> Handle(PutCommand request, CancellationToken cancellationToken)
        {
            var branch = _mapper.Map<Domain.Models.Settings.Branch>(request.model);
            var result = await _branchRepository.Put(branch);
            await _unitOfWork.CompleteAsync();
            return result;
        }
    }
}
