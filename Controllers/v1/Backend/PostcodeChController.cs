using Klacks_api.Datas;
using Klacks_api.Models.Settings;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Klacks_api.Controllers.V1.Backend;

public class PostcodeChController : BaseController
{
  private readonly DataBaseContext context;

  public PostcodeChController(DataBaseContext context)
  {
    this.context = context;
  }

  [HttpGet]
  public async Task<ActionResult<IEnumerable<PostcodeCH>>> GetPostcodeCh()
  {
    return await context.PostcodeCH.ToListAsync();
  }

  [HttpGet("{zip}")]
  public async Task<ActionResult<IEnumerable<PostcodeCH>>> GetPostcodeCh(int zip)
  {
    var postcodeCh = await context.PostcodeCH.Where(x => x.Zip == zip).ToListAsync();

    if (postcodeCh == null)
    {
      return NotFound();
    }

    return postcodeCh;
  }
}
