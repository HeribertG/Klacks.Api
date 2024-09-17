using FluentValidation;
using Klacks_api.Interfaces;
using Klacks_api.Queries.Clients;
using Klacks_api.Resources.Filter;

namespace Klacks_api.Validation.Clients
{
  public class GetTruncatedListQueryValidator : AbstractValidator<GetTruncatedListQuery>
  {
    public GetTruncatedListQueryValidator(IClientRepository repository)
    {
      RuleFor(query => query.Filter).NotNull().SetValidator(new FilterResourceValidator(repository));
    }
  }
}
