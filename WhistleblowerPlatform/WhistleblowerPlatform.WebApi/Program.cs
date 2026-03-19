using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using WhistleblowerPlatform.Application.Interfaces;
using WhistleblowerPlatform.Application.UseCases;
using WhistleblowerPlatform.Domain.Entities;
using WhistleblowerPlatform.Infrastructure;
using WhistleblowerPlatform.Infrastructure.Persistence;
using WhistleblowerPlatform.Infrastructure.Repositories;
using WhistleblowerPlatform.Infrastructure.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
builder.Services.AddSwaggerGen();

// Database
builder.Services.AddDbContext<WhistleblowerDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Identity (AddIdentityCore avoids cookie auth scheme conflicts with JWT)
builder.Services.AddIdentityCore<ApplicationUser>(options =>
{
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
    options.Password.RequiredLength = 12;
    options.SignIn.RequireConfirmedAccount = false;
})
.AddRoles<IdentityRole<Guid>>()
.AddEntityFrameworkStores<WhistleblowerDbContext>()
.AddDefaultTokenProviders();

// JWT
var jwtSettings = builder.Configuration.GetSection("JwtSettings").Get<JwtSettings>()!;
builder.Services.AddSingleton(jwtSettings);
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings.Issuer,
        ValidAudience = jwtSettings.Audience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.SecretKey))
    };
});
builder.Services.AddAuthorization();

builder.Services.AddHsts(options =>
{
    options.MaxAge = TimeSpan.FromDays(365);
    options.IncludeSubDomains = true;
});

// Auth services
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<IMfaService, MfaService>();
builder.Services.AddScoped<LoginUseCase>();

// Repositories
builder.Services.AddScoped<IReportRepository, ReportRepository>();
builder.Services.AddScoped<IInvestigatorRepository, InvestigatorRepository>();

//BlobStorage
var storagePath = Path.Combine(Directory.GetCurrentDirectory(), "blob-storage");
builder.Services.AddSingleton<IAttachmentStorageService>(new LocalBlobStorageService(storagePath));
builder.Services.AddFileSanitization();

//hCaptcha verification
builder.Services.AddHttpClient<IHCaptchaService, HCaptchaService>();

// Use cases
builder.Services.AddScoped<SubmitReportUseCase>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("BlazorClient", policy =>
    {
        policy.WithOrigins("https://localhost:7299")
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

var app = builder.Build();

app.UseCors("BlazorClient");

app.Use(async (context, next) =>
{
    context.Response.Headers["X-Content-Type-Options"] = "nosniff";
    context.Response.Headers["X-Frame-Options"] = "SAMEORIGIN";
    context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
    context.Response.Headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=(), payment=()";
    context.Response.Headers["Content-Security-Policy"] =
        "default-src 'self'; " +
        "script-src 'self' 'wasm-unsafe-eval' https://cdn.jsdelivr.net https://js.hcaptcha.com; " +
        "style-src 'self' 'unsafe-inline'; " +
        "img-src 'self' data: https://*.hcaptcha.com; " +
        "font-src 'self'; " +
        "connect-src 'self' https://hcaptcha.com https://*.hcaptcha.com; " +
        "frame-src https://newassets.hcaptcha.com; " +
        "frame-ancestors 'none'; " +
        "object-src 'none'; " +
        "base-uri 'self'";
    await next();
});

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseBlazorFrameworkFiles();
var provider = new Microsoft.AspNetCore.StaticFiles.FileExtensionContentTypeProvider();
provider.Mappings[".wasm"] = "application/wasm";
provider.Mappings[".br"] = "application/octet-stream";
provider.Mappings[".dat"] = "application/octet-stream";

app.UseStaticFiles(new StaticFileOptions
{
    ContentTypeProvider = provider
});

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.MapFallbackToFile("index.html");  // <-- ADD THIS: routes unknown paths to Blazor

app.Run();
