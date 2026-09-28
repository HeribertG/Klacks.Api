// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using FluentValidation;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Queries.Clients;
using Klacks.Api.Domain.DTOs.Filter;
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
