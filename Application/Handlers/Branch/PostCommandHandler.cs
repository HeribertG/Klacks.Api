using AutoMapper;
using Klacks.Api.Application.Commands.Settings.Branch;
using Klacks.Api.Application.Interfaces;
using MediatR;

namespace Klacks.Api.Application.Handlers.Branch
{
    public class PostCommandHandler : BaseHandler, IRequestHandler<PostCommand, Domain.Models.Settings.Branch?>
    {
        private readonly IBranchRepository _branchRepository;
        private readonly IMapper _mapper;
        private readonly IUnitOfWork _unitOfWork;

        public PostCommandHandler(
            IBranchRepository branchRepository,
            IMapper mapper,
            IUnitOfWork unitOfWork,
            ILogger<PostCommandHandler> logger)
            : base(logger)
        {
            _branchRepository = branchRepository;
            _mapper = mapper;
            _unitOfWork = unitOfWork;
        }

        public async Task<Domain.Models.Settings.Branch?> Handle(PostCommand request, CancellationToken cancellationToken)
        {
            var branch = _mapper.Map<Domain.Models.Settings.Branch>(request.model);
            await _branchRepository.Add(branch);
            await _unitOfWork.CompleteAsync();
            return branch;
        }
    }
}
