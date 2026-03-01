using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using WhistleblowerPlatform.Application.Interfaces;
using WhistleblowerPlatform.Domain.Enums;

namespace WhistleblowerPlatform.Infrastructure.Sanitization;

/// <summary>
/// Background service that polls for queued file attachments and processes
/// them through the sanitization pipeline.
/// 
/// Design choices:
///   - Polling loop (not event-driven) for simplicity and reliability
///   - SemaphoreSlim limits concurrent sanitizations to control memory usage
///   - Scoped DbContext per iteration (EF Core best practice for background services)
///   - Graceful shutdown via CancellationToken from the host
/// 
/// Configuration:
///   - Poll interval: how often to check for queued files (default: 30 seconds)
///   - Max concurrency: maximum parallel file sanitizations (default: 2)
///   - Both configurable via PlatformSettings or appsettings.json
/// </summary>
public class FileSanitizationBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<FileSanitizationBackgroundService> _logger;

    /// <summary>How often to poll the database for queued files.</summary>
    private readonly TimeSpan _pollInterval = TimeSpan.FromSeconds(30);

    /// <summary>Maximum number of files being sanitized concurrently.</summary>
    private readonly int _maxConcurrency = 2;

    public FileSanitizationBackgroundService(
        IServiceScopeFactory scopeFactory,
        ILogger<FileSanitizationBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "File sanitization service started (poll interval: {Interval}s, max concurrency: {MaxConcurrency})",
            _pollInterval.TotalSeconds, _maxConcurrency);

        // Semaphore limits how many files are being processed simultaneously.
        // This controls memory usage — each file is fully in memory during processing.
        // For a 1GB file limit, 2 concurrent = ~2GB peak memory.
        using var semaphore = new SemaphoreSlim(_maxConcurrency);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessQueuedFilesAsync(semaphore, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                // Graceful shutdown — exit the loop
                break;
            }
            catch (Exception ex)
            {
                // Log and continue — don't let one bad cycle kill the service
                _logger.LogError(ex, "Error in sanitization polling cycle");
            }

            // Wait before next poll
            try
            {
                await Task.Delay(_pollInterval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }

        _logger.LogInformation("File sanitization service stopped");
    }

    /// <summary>
    /// Finds all queued attachments and processes them concurrently
    /// (up to the max concurrency limit).
    /// </summary>
    private async Task ProcessQueuedFilesAsync(SemaphoreSlim semaphore, CancellationToken stoppingToken)
    {
        // Each poll cycle uses its own scope for the DbContext.
        // This is the recommended pattern for EF Core in background services:
        // DbContext is Scoped by default, but BackgroundService is a Singleton.
        // Creating a scope per cycle ensures fresh DbContext and proper disposal.
        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider
            .GetRequiredService<Persistence.WhistleblowerDbContext>();

        // Find all attachments waiting for sanitization
        var queuedIds = await dbContext.ReportAttachments
            .Where(a => a.SanitizationStatus == SanitizationStatus.Queued)
            .Select(a => a.AttachmentId)
            .ToListAsync(stoppingToken);

        if (queuedIds.Count == 0)
            return;

        _logger.LogInformation("Found {Count} files queued for sanitization", queuedIds.Count);

        // Process files concurrently with bounded parallelism.
        // Each file gets its own scope (and therefore its own DbContext)
        // so they don't interfere with each other.
        var tasks = queuedIds.Select(async attachmentId =>
        {
            await semaphore.WaitAsync(stoppingToken);
            try
            {
                // Each file processed in its own DI scope
                using var fileScope = _scopeFactory.CreateScope();
                var sanitizationService = fileScope.ServiceProvider
                    .GetRequiredService<IFileSanitizationService>();

                await sanitizationService.SanitizeAttachmentAsync(attachmentId, stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to sanitize attachment {AttachmentId}", attachmentId);
            }
            finally
            {
                semaphore.Release();
            }
        });

        await Task.WhenAll(tasks);
    }
}
