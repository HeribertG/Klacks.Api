using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Models.Settings;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Klacks.Api.Presentation.Controllers.UserBackend.Settings;

[Authorize(Roles = "Admin")]
public class PostcodeChController : BaseController
{
    private readonly IPostcodeChRepository _postcodeChRepository;

    public PostcodeChController(IPostcodeChRepository postcodeChRepository)
    {
        _postcodeChRepository = postcodeChRepository;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<PostcodeCH>>> GetPostcodeCh()
    {
        return await _postcodeChRepository.GetAllAsync();
    }

    [HttpGet("{zip}")]
    public async Task<ActionResult<IEnumerable<PostcodeCH>>> GetPostcodeCh(int zip)
    {
        var postcodeCh = await _postcodeChRepository.GetByZipAsync(zip);

        if (postcodeCh == null || postcodeCh.Count == 0)
        {
            return NotFound();
        }

        return postcodeCh;
    }
}
