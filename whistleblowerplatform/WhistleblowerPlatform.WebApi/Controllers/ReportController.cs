using Microsoft.AspNetCore.Mvc;
using WhistleblowerPlatform.Application.DTOs;
using WhistleblowerPlatform.Application.Interfaces;
using WhistleblowerPlatform.Application.UseCases;

namespace WhistleblowerPlatform.WebApi.Controllers;

[ApiController]
[Route("api/reports")]
public class ReportController : ControllerBase
{
    private readonly SubmitReportUseCase _submitReportUseCase;
    private readonly IReportRepository _reportRepository;

    public ReportController(SubmitReportUseCase submitReportUseCase, IReportRepository reportRepository)
    {
        _submitReportUseCase = submitReportUseCase;
        _reportRepository = reportRepository;
    }

    [HttpPost]
    public async Task<IActionResult> SubmitReport([FromBody] SubmitReportRequest request)
    {
        var result = await _submitReportUseCase.ExecuteAsync(request);
        return Created($"/api/reports/{result.CaseNumber}", result);
    }

    [HttpGet("categories")]
    public async Task<IActionResult> GetCategories()
    {
        var categories = await _reportRepository.GetActiveCategoriesAsync();
        return Ok(categories);
    }
}
