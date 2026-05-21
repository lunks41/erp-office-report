using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Telerik.Reporting.Cache.File;
using Telerik.Reporting.Services;
using TelerikReportingRestService.Extensions;

EnableTracing();
var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddCors(options =>
{
    // Reporting viewer can be hosted by different applications/ports.
    // Keep CORS open for the reports API to avoid cross-origin blocks.
    options.AddPolicy("AllowOrigin", policy =>
        policy.AllowAnyOrigin()
            .AllowAnyHeader()
            .AllowAnyMethod()
            .WithExposedHeaders("*"));
});

// appsettings.json is gitignored — copy appsettings.example.json locally.
// WebApplication.CreateBuilder already loads appsettings.json, appsettings.{Environment}.json, and environment variables.
builder.Services.RegisterService(builder.Configuration);

// Do not call AddNewtonsoftJson() globally: Telerik Reporting 2026 Q1+ REST uses System.Text.Json.
builder.Services.AddControllers();
builder.Services.AddRazorPages();

var reportsPath = System.IO.Path.Combine(builder.Environment.ContentRootPath, "Reports");

// Configure dependencies for ReportsController.
builder.Services.TryAddSingleton<IReportServiceConfiguration>(sp =>
    new ReportServiceConfiguration
    {
        // The default ReportingEngineConfiguration will be initialized from appsettings.json or appsettings.{EnvironmentName}.json:
        ReportingEngineConfiguration = sp.GetService<IConfiguration>(),

        // In case the ReportingEngineConfiguration needs to be loaded from a specific configuration file, use the approach below:
        //ReportingEngineConfiguration = ResolveSpecificReportingConfiguration(sp.GetService<IWebHostEnvironment>()),
        HostAppId = "TelerikReportingRestService",
        Storage = new FileStorage(),
        ReportSourceResolver = new TypeReportSourceResolver()
            .AddFallbackResolver(new UriReportSourceResolver(reportsPath))
    });

// Configures JWT bearer authentication to protect API endpoints.
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,                       // Ensure token issuer matches
            ValidateAudience = true,                     // Ensure token audience matches
            ValidateLifetime = true,                     // Check token hasn't expired
            ValidateIssuerSigningKey = true,             // Verify the signing key
            ValidAudience = builder.Configuration["JWT:ValidAudience"],
            ValidIssuer = builder.Configuration["JWT:ValidIssuer"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(
                    builder.Configuration["JWT:SecretKey"] ?? string.Empty))
        };

        // Configure JWT handler for SignalR
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                // Allow JWT authentication for SignalR hub connections
                var accessToken = context.Request.Query["access_token"];
                var path = context.HttpContext.Request.Path;

                if (!string.IsNullOrEmpty(accessToken) &&
                    path.StartsWithSegments("/notificationHub"))
                {
                    context.Token = accessToken;
                }

                return Task.CompletedTask;
            }
        };
    });

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
}

app.UseStaticFiles();
app.UseRouting();
// CORS must run after UseRouting and before UseAuthentication/UseAuthorization
// so preflight (OPTIONS) and API responses include Access-Control-* headers.
app.UseCors("AllowOrigin");
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
