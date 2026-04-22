using Microsoft.AspNetCore.Mvc;
using WhistleblowerPlatform.Application.DTOs;
using WhistleblowerPlatform.Application.Interfaces;
using WhistleblowerPlatform.Application.UseCases;
using WhistleblowerPlatform.Domain.Entities;
using WhistleblowerPlatform.Infrastructure.Persistence;

namespace WhistleblowerPlatform.WebApi.Controllers;

[ApiController]
[Route("api/reports")]
public class ReportController : ControllerBase
{
    private readonly SubmitReportUseCase _submitReportUseCase;
    private readonly IReportRepository _reportRepository;
    private readonly WhistleblowerDbContext _dbContext;

    public ReportController(SubmitReportUseCase submitReportUseCase, IReportRepository reportRepository, WhistleblowerDbContext dbContext)
    {
        _submitReportUseCase = submitReportUseCase;
        _reportRepository = reportRepository;
        _dbContext = dbContext;
    }

    [HttpPost]
    public async Task<IActionResult> SubmitReport([FromBody] SubmitReportRequest request)
    {
        try
        {
            var result = await _submitReportUseCase.ExecuteAsync(request);

            _dbContext.AuditLogs.Add(new AuditLog
            {
                ActorType = 0,
                Action = "ReportSubmitted",
                TargetEntity = "Reports",
                TargetId = result.CaseNumber,
                Ipaddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
                UserAgent = Request.Headers.UserAgent.ToString(),
                Timestamp = DateTime.UtcNow
            });
            await _dbContext.SaveChangesAsync();

            return Created($"/api/reports/{result.CaseNumber}", result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { errors = ex.Message });
        }
    }

    [HttpGet("categories")]
    public async Task<IActionResult> GetCategories()
    {
        var categories = await _reportRepository.GetActiveCategoriesAsync();
        return Ok(categories);
    }

    /// <summary>
    /// Get the investigator's public key for client-side encryption.
    /// No authentication required.
    /// </summary>
    [HttpGet("/api/config/public-key")]
    public async Task<IActionResult> GetPublicKey()
    {
        var publicKey = await _reportRepository.GetInvestigatorPublicKeyAsync();
        if (publicKey == null)
            return NotFound("No investigator public key found.");

        return Ok(new { publicKey = Convert.ToBase64String(publicKey) });
    }
}
