// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Models.Settings;
using Klacks.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Infrastructure.Repositories.Settings;

public class PostcodeChRepository : IPostcodeChRepository
{
    private readonly DataBaseContext _context;

    public PostcodeChRepository(DataBaseContext context)
    {
        _context = context;
    }

    public async Task<List<PostcodeCH>> GetAllAsync()
    {
        return await _context.PostcodeCH.ToListAsync();
    }

    public async Task<List<PostcodeCH>> GetByZipAsync(int zip)
    {
        return await _context.PostcodeCH.Where(x => x.Zip == zip).ToListAsync();
    }
}
