// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using FluentValidation;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Queries.Clients;
using Klacks.Api.Application.DTOs.Filter;

namespace Klacks.Api.Application.Validation.Clients
{
    public class GetTruncatedListQueryValidator : AbstractValidator<GetTruncatedListQuery>
    {
        public GetTruncatedListQueryValidator(IClientRepository repository)
        {
            RuleFor(query => query.Filter).NotNull().SetValidator(new FilterResourceValidator(repository));
        }
    }
}
