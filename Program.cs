using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Telerik.Reporting.Cache.File;
using Telerik.Reporting.Services;
using apireport.Extensions;
using System.Linq;

EnableTracing();
var builder = WebApplication.CreateBuilder(args);
var isDev = builder.Environment.IsDevelopment();

// Add services to the container.

builder.Services.AddCors(options =>
{
    var allowedOriginsRaw = builder.Configuration["Cors:AllowedOrigins"]
                            ?? builder.Configuration["CORS_ALLOWED_ORIGINS"];

    var allowedOrigins = (allowedOriginsRaw ?? string.Empty)
        .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
        .ToArray();

    options.AddPolicy("AllowOrigin", policy =>
    {
        if (isDev)
        {
            policy.AllowAnyOrigin();
        }
        else if (allowedOrigins.Length == 0)
        {
            policy.SetIsOriginAllowed(_ => false);
        }
        else
        {
            policy.SetIsOriginAllowed(origin =>
                allowedOrigins.Contains(origin, StringComparer.OrdinalIgnoreCase));
        }

        policy.AllowAnyHeader()
              .AllowAnyMethod()
              .WithExposedHeaders("*");
    });
});

// appsettings.json is gitignored — copy appsettings.example.json locally.
// WebApplication.CreateBuilder already loads appsettings.json, appsettings.{Environment}.json, and environment variables.
builder.Services.RegisterService(builder.Configuration);

// Do not call AddNewtonsoftJson() globally: Telerik Reporting 2026 Q1+ REST uses System.Text.Json.
builder.Services.AddControllers();
builder.Services.AddRazorPages();

var reportsPath = System.IO.Path.Combine(builder.Environment.ContentRootPath, "Reports");

// FileStorage() defaults to C:\WINDOWS\TEMP\{HostAppId}\... which IIS app-pool
// identities often cannot write. Keep cache under the app so permissions are controllable.
var reportCachePath = Path.Combine(builder.Environment.ContentRootPath, "App_Data", "ReportCache");
Directory.CreateDirectory(reportCachePath);

// Configure dependencies for ReportsController.
builder.Services.TryAddSingleton<IReportServiceConfiguration>(sp =>
{
    var fallbackResolver = new TypeReportSourceResolver()
        .AddFallbackResolver(new UriReportSourceResolver(reportsPath));

    var reportSourceResolver = new RegIdReportSourceResolver(
        fallbackResolver,
        sp.GetRequiredService<IHttpContextAccessor>(),
        sp.GetRequiredService<ReportConnectionResolver>(),
        sp.GetRequiredService<ILogger<RegIdReportSourceResolver>>());

    // Support both erpkendoreport and erpofficereport via config
    var hostAppId = builder.Configuration["ReportingConfig:HostAppId"] ?? "erpkendoreport";

    return new ReportServiceConfiguration
    {
        // The default ReportingEngineConfiguration will be initialized from appsettings.json or appsettings.{EnvironmentName}.json:
        ReportingEngineConfiguration = sp.GetService<IConfiguration>(),

        // In case the ReportingEngineConfiguration needs to be loaded from a specific configuration file, use the approach below:
        //ReportingEngineConfiguration = ResolveSpecificReportingConfiguration(sp.GetService<IWebHostEnvironment>()),
        HostAppId = hostAppId,
        Storage = new FileStorage(reportCachePath),
        ReportSourceResolver = reportSourceResolver,
    };
});

// Configures JWT bearer authentication to protect API endpoints.
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        // Local HTTP (erp-office → api-report) must accept Bearer tokens without HTTPS.
        options.RequireHttpsMetadata = false;
        options.SaveToken = true;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidAudience = builder.Configuration["JWT:ValidAudience"],
            ValidIssuer = builder.Configuration["JWT:ValidIssuer"],
            ClockSkew = TimeSpan.FromMinutes(10),
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(
                    builder.Configuration["JWT:SecretKey"] ?? string.Empty))
        };

        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                var path = context.HttpContext.Request.Path;

                if (!string.IsNullOrEmpty(accessToken) &&
                    (path.StartsWithSegments("/notificationHub") ||
                     path.StartsWithSegments("/api/reports") ||
                     path.StartsWithSegments("/api/Reports")))
                {
                    context.Token = accessToken;
                }

                // Telerik may send Authorization without the Bearer scheme.
                if (string.IsNullOrEmpty(context.Token))
                {
                    var header = context.Request.Headers.Authorization.ToString();
                    if (!string.IsNullOrWhiteSpace(header) &&
                        !header.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                    {
                        context.Token = header.Trim();
                    }
                }

                return Task.CompletedTask;
            },
            OnAuthenticationFailed = context =>
            {
                var logger = context.HttpContext.RequestServices
                    .GetRequiredService<ILoggerFactory>()
                    .CreateLogger("JwtBearer");
                logger.LogWarning(
                    context.Exception,
                    "JWT authentication failed for {Path}: {Message}",
                    context.Request.Path,
                    context.Exception.Message);
                return Task.CompletedTask;
            }
        };
    });

var app = builder.Build();

// Configure the HTTP request pipeline.
// With endpoint routing, UseCors must run after UseRouting and before UseAuthentication/UseAuthorization.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
}
app.UseStaticFiles();

app.UseRouting();

app.UseCors("AllowOrigin");

// Authentication and Authorization middleware
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

/// <summary>
/// Uncomment the lines to enable tracing in the current application.
/// The trace log will be persisted in a file named log.txt in the application root directory.
/// </summary>
static void EnableTracing()
{
    // System.Diagnostics.Trace.Listeners.Add(new System.Diagnostics.TextWriterTraceListener(File.CreateText("log.txt")));
    // System.Diagnostics.Trace.AutoFlush = true;
}
