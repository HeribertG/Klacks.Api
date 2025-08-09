using Klacks.Api.Exceptions;
using Klacks.Api.Queries;
using Klacks.Api.Queries.Absences;
using Klacks.Api.Presentation.Resources.Filter;
using Klacks.Api.Presentation.Resources.Schedules;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Text;

namespace Klacks.Api.Presentation.Controllers.v1.UserBackend;

public class AbsencesController : InputBaseController<AbsenceResource>
{
    private readonly ILogger<AbsencesController> _logger;

    public AbsencesController(IMediator Mediator, ILogger<AbsencesController> logger)
          : base(Mediator, logger)
    {
        _logger = logger;
    }

    [HttpGet("CreateExcelFile/{language}")]
    public async Task<FileContentResult> CreateExcelFile([FromRoute] string language)
    {
        _logger.LogInformation($"Starting to create Excel file for absences in language: {language}");

        try
        {
            var res = await Mediator.Send(new CreateExcelFileQuery(language));
            if (res.Success)
            {
                string fileName = res.Messages;
                byte[] result = System.IO.File.ReadAllBytes(fileName);
                _logger.LogInformation($"Excel file for absences created successfully: {fileName}");
                return File(result, "application/octet-stream", "Absences.xlsx");
            }
            else
            {
                _logger.LogWarning($"Failed to create Excel file for absences: {res.Messages}");
                return File(Encoding.UTF8.GetBytes(res.Messages), "text/plain");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error occurred while creating Excel file for absences in language: {language}");
            throw new InvalidRequestException($"Error occurred while creating Excel file for absences in language: {language} " + ex.Message);
        }
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<AbsenceResource>>> GetAbsence()
    {
        _logger.LogInformation("Request received to get list of all absences.");
        var absences = await Mediator.Send(new ListQuery<AbsenceResource>());
        _logger.LogInformation($"Retrieved {absences.Count()} absences.");
        return Ok(absences);
    }

    [HttpPost("GetSimpleAbsenceList")]
    public async Task<TruncatedAbsence> GetSimpleAbsenceList([FromBody] AbsenceFilter filter)
    {
        _logger.LogInformation($"Received request for GetSimpleAbsenceList with filter: {JsonConvert.SerializeObject(filter)}");

        try
        {
            var truncatedAbsence = await Mediator.Send(new TruncatedListQuery(filter));
            _logger.LogInformation($"Retrieved truncated list of absences.");
            return truncatedAbsence;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while retrieving truncated list of absences.");
            throw new InvalidRequestException("Error occurred while retrieving truncated list of absences. " + ex.Message);
        }
    }
}
