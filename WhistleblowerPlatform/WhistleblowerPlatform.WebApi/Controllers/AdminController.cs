using Microsoft.AspNetCore.Mvc;
using UUIDNext;
using WhistleblowerPlatform.Infrastructure.Persistence;

namespace WhistleblowerPlatform.WebApi.Controllers;

[ApiController]
[Route("api/admin")]
public class AdminController : ControllerBase
{
    private readonly WhistleblowerDbContext _context;

    public AdminController(WhistleblowerDbContext context)
    {
        _context = context; 
    }
    [HttpPost("test-investigator")]
    public async Task<IActionResult> CreateTestInvestigator([FromBody] TestInvestigatorRequest request)
    {
        var investigator = new Domain.Entities.Investigator
        {
            InvestigatorId = Uuid.NewSequential(),
            Email = request.Email,
            PasswordHash = request.PasswordHash,
            Mfasecret = request.MfaSecret,
            Mfaenabled = false,
            PublicKey = Convert.FromBase64String(request.PublicKey),
            EncryptedPrivateKey = Convert.FromBase64String(request.EncryptedPrivateKey),
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Investigators.Add(investigator);
        await _context.SaveChangesAsync();

        return Ok(new { message = "Test investigator created" });
    }

    
}

public class TestInvestigatorRequest
{
    public string Email { get; set; } = String.Empty;
    public string PasswordHash { get; set; } = String.Empty;
    public string MfaSecret { get; set; } = String.Empty;
    public string PublicKey { get; set; } = String.Empty;
    public string EncryptedPrivateKey { get; set; } = String.Empty;
}
