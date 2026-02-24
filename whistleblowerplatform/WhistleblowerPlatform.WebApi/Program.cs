using Microsoft.EntityFrameworkCore;
using WhistleblowerPlatform.Application.Interfaces;
using WhistleblowerPlatform.Application.UseCases;
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

// Repositories
builder.Services.AddScoped<IReportRepository, ReportRepository>();

//BlobStorage
var storagePath = Path.Combine(Directory.GetCurrentDirectory(), "blob-storage");
builder.Services.AddSingleton<IAttachmentStorageService>(new LocalBlobStorageService(storagePath));

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


if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
