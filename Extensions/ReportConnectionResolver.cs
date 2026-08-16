using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace apireport.Extensions
{
    public sealed class ReportConnectionInfo
    {
        public string? RegIdFromHeader { get; init; }
        public string? RegIdUsed { get; init; }
        public string ConnectionStringName { get; init; } = string.Empty;
        public string? DataSource { get; init; }
        public string? InitialCatalog { get; init; }
        public IReadOnlyList<string> ConfiguredConnectionNames { get; init; } = [];
    }

    /// <summary>
    /// Resolves SQL connection strings from X-Reg-Id via regCompany.json (same pattern as api-core).
    /// </summary>
    public class ReportConnectionResolver
    {
        private readonly IConfiguration _configuration;
        private readonly DBGetConnection _dbGetConnection;

        public ReportConnectionResolver(IConfiguration configuration, IHostEnvironment hostEnvironment)
        {
            _configuration = configuration;
            _dbGetConnection = new DBGetConnection(hostEnvironment.ContentRootPath);
        }

        public string? ResolveConnectionString(IHeaderDictionary? headers)
        {
            return ResolveConnectionString(ResolveRegId(headers, null));
        }

        /// <summary>
        /// Returns the appsettings connection string name (e.g. DbConnection) for Telerik SqlDataSource.
        /// Reports are authored with a named reference like Reporting.AHHA; swapping the name preserves provider resolution.
        /// </summary>
        public string ResolveConnectionStringName(IHeaderDictionary? headers)
        {
            return ResolveConnectionStringName(ResolveRegId(headers, null));
        }

        public string? ResolveRegId(IHeaderDictionary? headers, IDictionary<string, object>? parameters)
        {
            if (headers != null && headers.TryGetValue("X-Reg-Id", out var regIdValues))
            {
                var headerRegId = regIdValues.FirstOrDefault();
                if (!string.IsNullOrWhiteSpace(headerRegId))
                    return headerRegId;
            }

            if (parameters != null)
            {
                foreach (var key in new[] { "regId", "RegId" })
                {
                    if (!parameters.TryGetValue(key, out var value) || value == null)
                        continue;

                    var paramRegId = value.ToString();
                    if (!string.IsNullOrWhiteSpace(paramRegId))
                        return paramRegId;
                }
            }

            return null;
        }

        public string? ResolveConnectionString(string? regId)
        {
            var name = ResolveConnectionStringName(regId);
            return GetConnectionStringByName(name);
        }

        public string ResolveConnectionStringName(string? regId)
        {
            if (!string.IsNullOrWhiteSpace(regId))
            {
                try
                {
                    var connectionStringName = _dbGetConnection.GetconnectionDB(regId);
                    if (!string.IsNullOrEmpty(connectionStringName)
                        && !string.IsNullOrEmpty(GetConnectionStringByName(connectionStringName)))
                    {
                        return connectionStringName;
                    }
                }
                catch
                {
                    // regCompany.json lookup failed — fall through to defaults
                }
            }

            if (!string.IsNullOrEmpty(GetConnectionStringByName("DbConnection")))
                return "DbConnection";

            return "Reporting.AHHA";
        }

        public ReportConnectionInfo DescribeConnection(
            IHeaderDictionary? headers,
            IDictionary<string, object>? parameters = null,
            string? regIdOverride = null)
        {
            string? headerRegId = null;
            if (headers != null && headers.TryGetValue("X-Reg-Id", out var regIdValues))
                headerRegId = regIdValues.FirstOrDefault();

            var regIdUsed = !string.IsNullOrWhiteSpace(regIdOverride)
                ? regIdOverride
                : ResolveRegId(headers, parameters);
            var connectionStringName = !string.IsNullOrWhiteSpace(regIdUsed)
                ? ResolveConnectionStringName(regIdUsed)
                : ResolveConnectionStringName(headers);

            var connectionString = GetConnectionStringByName(connectionStringName);
            string? dataSource = null;
            string? catalog = null;

            if (!string.IsNullOrEmpty(connectionString))
            {
                try
                {
                    var builder = new SqlConnectionStringBuilder(connectionString);
                    dataSource = builder.DataSource;
                    catalog = builder.InitialCatalog;
                }
                catch
                {
                    // ignore malformed connection string in diagnostics
                }
            }

            var configuredNames = _configuration.GetSection("ConnectionStrings")
                .GetChildren()
                .Select(c => c.Key)
                .ToList();

            return new ReportConnectionInfo
            {
                RegIdFromHeader = headerRegId,
                RegIdUsed = regIdUsed,
                ConnectionStringName = connectionStringName,
                DataSource = dataSource,
                InitialCatalog = catalog,
                ConfiguredConnectionNames = configuredNames,
            };
        }

        private string? GetConnectionStringByName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return null;

            var direct = _configuration.GetConnectionString(name);
            if (!string.IsNullOrEmpty(direct))
                return direct;

            var section = _configuration.GetSection("ConnectionStrings");
            foreach (var child in section.GetChildren())
            {
                if (!string.Equals(child.Key, name, StringComparison.OrdinalIgnoreCase))
                    continue;

                return child.Value ?? child["ConnectionString"];
            }

            return null;
        }
    }
}
