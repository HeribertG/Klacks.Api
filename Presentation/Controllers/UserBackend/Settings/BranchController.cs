using Klacks.Api.Application.Commands.Settings.Branch;
using Klacks.Api.Domain.Models.Settings;
using Klacks.Api.Infrastructure.Mediator;
using Microsoft.AspNetCore.Mvc;

namespace Klacks.Api.Presentation.Controllers.UserBackend.Settings;

public class BranchController : BaseController
{
    private readonly IMediator mediator;
    private readonly ILogger<BranchController> _logger;

    public BranchController(IMediator mediator, ILogger<BranchController> logger)
    {
        this.mediator = mediator;
        this._logger = logger;
    }

    [HttpPost("AddBranch")]
    public async Task<Branch> AddBranch([FromBody] Branch branch)
    {
        var res = await mediator.Send(new PostCommand(branch));
        return res;
    }

    [HttpGet("GetBranch/{id}")]
    public async Task<ActionResult<Branch?>> GetBranch(Guid id)
    {
        var branch = await mediator.Send(new Application.Queries.Settings.Branch.GetQuery(id));
        if (branch == null)
        {
            return NotFound();
        }

        return branch;
    }

    [HttpGet("GetBranchList")]
    public async Task<IEnumerable<Branch>> GetBranchListAsync()
    {
        var branches = await mediator.Send(new Application.Queries.Settings.Branch.ListQuery());
        return branches;
    }

    [HttpPut("PutBranch")]
    public async Task<Branch> PutBranch([FromBody] Branch branch)
    {
        var res = await mediator.Send(new PutCommand(branch));
        return res;
    }

    [HttpDelete("DeleteBranch/{id}")]
    public async Task<ActionResult> DeleteBranch(Guid id)
    {
        _logger.LogInformation("DeleteBranch called with ID: {Id}", id);
        await mediator.Send(new DeleteCommand(id));
        _logger.LogInformation("DeleteBranch completed for ID: {Id}", id);
        return NoContent();
    }
}
