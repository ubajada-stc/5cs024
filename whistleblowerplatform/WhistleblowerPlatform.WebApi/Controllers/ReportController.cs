using Microsoft.AspNetCore.Mvc;
using WhistleblowerPlatform.Application.DTOs;
using WhistleblowerPlatform.Application.UseCases;

namespace WhistleblowerPlatform.WebApi.Controllers;

[ApiController]
[Route("api/reports")]
public class ReportController : ControllerBase
{
    private readonly SubmitReportUseCase _submitReportUseCase;

    public ReportController(SubmitReportUseCase submitReportUseCase)
    {
        _submitReportUseCase = submitReportUseCase;
    }

    [HttpPost]
    public async Task<IActionResult> SubmitReport([FromBody] SubmitReportRequest request)
    {
        var result = await _submitReportUseCase.ExecuteAsync(request);
        return Created($"/api/reports/{result.CaseNumber}", result);
    }
}
