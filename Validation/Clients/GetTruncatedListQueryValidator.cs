using FluentValidation;
using Klacks.Api.Interfaces;
using Klacks.Api.Queries.Clients;
using Klacks.Api.Presentation.Resources.Filter;

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
