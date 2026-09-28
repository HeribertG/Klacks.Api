// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Domain.Common;
using Klacks.Api.Infrastructure.Persistence;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Models.Staffs;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Infrastructure.Repositories
{
    public class AnnotationRepository : BaseRepository<Annotation>, IAnnotationRepository
    {
        public AnnotationRepository(DataBaseContext context, ILogger<Annotation> logger)
            : base(context, logger)
        {
        }

        public async Task<List<Annotation>> SimpleList(Guid id)
        {
            return await this.context.Annotation.Where(x => x.ClientId == id).ToListAsync();
        }
    }
}
