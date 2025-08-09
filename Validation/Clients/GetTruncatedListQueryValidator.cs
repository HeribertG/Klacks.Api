using FluentValidation;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Queries.Clients;
using Klacks.Api.Presentation.DTOs.Filter;

namespace Klacks.Api.Validation.Clients
{
    public class GetTruncatedListQueryValidator : AbstractValidator<GetTruncatedListQuery>
    {
        public GetTruncatedListQueryValidator(IClientRepository repository)
        {
            RuleFor(query => query.Filter).NotNull().SetValidator(new FilterResourceValidator(repository));
        }
    }
}
