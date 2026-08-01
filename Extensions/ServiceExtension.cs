using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Primitives;

namespace erpofficereport.Extensions
{
    public static class ServiceExtension
    {
        public static IServiceCollection RegisterService(this IServiceCollection serviceCollection, IConfiguration configuration)
        {
            serviceCollection.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();

            serviceCollection.AddDbContext<ApplicationDbContext>((serviceProvider, options) =>
            {
                DBGetConnection dBGetConnection = new DBGetConnection();
                string RegId = string.Empty;
                var connectionString = string.Empty;
                var getConnectionStringName = string.Empty;

                var httpContextAccessor = serviceProvider.GetRequiredService<IHttpContextAccessor>();
                StringValues regIdHeader = httpContextAccessor.HttpContext.Request.Headers["X-Reg-Id"];
                if (regIdHeader.Count > 0 && !StringValues.IsNullOrEmpty(regIdHeader))
                {
                    RegId = regIdHeader[0] ?? string.Empty;
                }

                if (!string.IsNullOrEmpty(RegId))
                    getConnectionStringName = dBGetConnection.GetconnectionDB(RegId);

                if (!string.IsNullOrEmpty(getConnectionStringName))
                    connectionString = configuration.GetConnectionString(getConnectionStringName);

                //var connectionString = configuration.GetConnectionString("DbConnection");
                options.UseSqlServer(connectionString,
                            b => b.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName));
            });

            return serviceCollection;
        }
    }
}