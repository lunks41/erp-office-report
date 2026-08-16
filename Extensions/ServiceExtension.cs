using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace apireport.Extensions
{
    public static class ServiceExtension
    {
        public static IServiceCollection RegisterService(this IServiceCollection serviceCollection, IConfiguration configuration)
        {
            serviceCollection.AddHttpContextAccessor();
            serviceCollection.AddSingleton<ReportConnectionResolver>(sp =>
                new ReportConnectionResolver(
                    sp.GetRequiredService<IConfiguration>(),
                    sp.GetRequiredService<IHostEnvironment>()));

            serviceCollection.AddDbContext<ApplicationDbContext>((serviceProvider, options) =>
            {
                var connectionResolver = serviceProvider.GetRequiredService<ReportConnectionResolver>();
                var httpContextAccessor = serviceProvider.GetRequiredService<IHttpContextAccessor>();
                var connectionString = connectionResolver.ResolveConnectionString(
                    httpContextAccessor.HttpContext?.Request.Headers) ?? string.Empty;

                options.UseSqlServer(
                    connectionString,
                    b => b.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName));
            });

            return serviceCollection;
        }
    }
}
