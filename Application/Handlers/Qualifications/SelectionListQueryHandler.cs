// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Handler for the qualification selection list. Applies ACTIVE_INDUSTRIES filtering so choice
/// lists only offer qualifications of active industries plus industry-less rows; without the
/// setting the full list is returned unfiltered.
/// </summary>
/// <param name="repository">Provides ordered, non-tracking qualification queries</param>
/// <param name="activeIndustriesProvider">Resolves the active industry slugs from settings</param>

using Klacks.Api.Application.Queries.Qualifications;
using Klacks.Api.Domain.Interfaces.Associations;
using Klacks.Api.Domain.Interfaces.Settings;
using Klacks.Api.Domain.Models.Staffs;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.Qualifications;

public sealed class SelectionListQueryHandler : BaseHandler, IRequestHandler<SelectionListQuery, IEnumerable<Qualification>>
{
    private readonly IQualificationRepository _repository;
    private readonly IActiveIndustriesProvider _activeIndustriesProvider;

    public SelectionListQueryHandler(
        IQualificationRepository repository,
        IActiveIndustriesProvider activeIndustriesProvider,
        ILogger<SelectionListQueryHandler> logger)
        : base(logger)
    {
        _repository = repository;
        _activeIndustriesProvider = activeIndustriesProvider;
    }

    public async Task<IEnumerable<Qualification>> Handle(SelectionListQuery request, CancellationToken cancellationToken)
    {
        return await ExecuteAsync(async () =>
        {
            var activeIndustrySlugs = await _activeIndustriesProvider.GetActiveIndustrySlugsAsync();
            return activeIndustrySlugs == null
                ? await _repository.GetAllAsync(cancellationToken)
                : await _repository.GetSelectableAsync(activeIndustrySlugs, cancellationToken);
        }, nameof(Handle));
    }
}
