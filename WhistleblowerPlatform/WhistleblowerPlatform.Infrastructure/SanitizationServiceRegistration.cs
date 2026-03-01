using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WhistleblowerPlatform.Application.Interfaces;
using WhistleblowerPlatform.Infrastructure.Sanitization;

namespace WhistleblowerPlatform.Infrastructure;

/// <summary>
/// Extension method to register all file sanitization services in the DI container.
/// Call from WebApi/Program.cs: builder.Services.AddFileSanitization();
/// </summary>
public static class SanitizationServiceRegistration
{
    public static IServiceCollection AddFileSanitization(this IServiceCollection services)
    {
        // Register individual sanitizers (strategy pattern).
        // Each sanitizer handles a group of MIME types.
        // To add a new file type, just create a new IFileSanitizer implementation
        // and register it here — no other code changes needed.
        services.AddSingleton<IFileSanitizer, ImageSanitizer>();
        services.AddSingleton<IFileSanitizer, PdfSanitizer>();
        services.AddSingleton<IFileSanitizer, OfficeSanitizer>();
        services.AddSingleton<IFileSanitizer, AudioVideoSanitizer>();

        // Register the orchestration service (scoped — uses DbContext)
        services.AddScoped<IFileSanitizationService, FileSanitizationService>();

        // Register the background service (singleton — runs for app lifetime)
        services.AddHostedService<FileSanitizationBackgroundService>();

        return services;
    }
}