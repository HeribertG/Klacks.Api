using Klacks.Api.Application.Queries.Settings.CalendarRules;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Models.Settings;
using Klacks.Api.Infrastructure.Mediator;
using Klacks.Api.Application.DTOs.Settings;
using Microsoft.Extensions.Logging;

namespace Klacks.Api.Application.Handlers.Settings.CalendarRule;

public class ValidateRuleQueryHandler : IRequestHandler<ValidateRuleQuery, ValidateCalendarRuleResponse>
{
    private readonly IHolidaysListCalculator _calculator;
    private readonly ILogger<ValidateRuleQueryHandler> _logger;

    public ValidateRuleQueryHandler(IHolidaysListCalculator calculator, ILogger<ValidateRuleQueryHandler> logger)
    {
        _calculator = calculator;
        _logger = logger;
    }

    public Task<ValidateCalendarRuleResponse> Handle(ValidateRuleQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Validating calendar rule: {Rule}, SubRule: {SubRule}, Year: {Year}",
            request.Rule, request.SubRule, request.Year);

        var response = new ValidateCalendarRuleResponse
        {
            Year = request.Year ?? DateTime.Now.Year
        };

        if (string.IsNullOrWhiteSpace(request.Rule))
        {
            response.IsValid = false;
            response.ErrorMessage = "Rule cannot be empty";
            return Task.FromResult(response);
        }

        try
        {
            _calculator.Clear();
            _calculator.CurrentYear = response.Year;

            var testRule = new Domain.Models.Settings.CalendarRule
            {
                Rule = request.Rule,
                SubRule = request.SubRule ?? string.Empty,
                IsMandatory = true
            };

            _calculator.Add(testRule);
            _calculator.ComputeHolidays();

            if (_calculator.HolidayList.Count > 0)
            {
                var holiday = _calculator.HolidayList[0];
                response.IsValid = true;
                response.CalculatedDate = holiday.CurrentDate;
                response.FormattedDate = holiday.FormatDate;
                response.DayOfWeek = holiday.CurrentDate.DayOfWeek.ToString();
            }
            else
            {
                response.IsValid = false;
                response.ErrorMessage = "Rule did not produce a valid date";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating calendar rule: {Rule}", request.Rule);
            response.IsValid = false;
            response.ErrorMessage = $"Invalid rule format: {ex.Message}";
        }

        return Task.FromResult(response);
    }
}
