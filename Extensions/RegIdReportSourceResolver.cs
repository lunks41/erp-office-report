using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Telerik.Reporting;
using Telerik.Reporting.Services;

namespace apireport.Extensions
{
    /// <summary>
    /// REST report resolver: maps X-Reg-Id / regId parameter to the tenant connection name
    /// and applies it to every SqlDataSource before Telerik renders the report.
    /// </summary>
    public class RegIdReportSourceResolver : IReportSourceResolver
    {
        private readonly IReportSourceResolver _parentResolver;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ReportConnectionResolver _connectionResolver;
        private readonly ILogger<RegIdReportSourceResolver> _logger;

        public RegIdReportSourceResolver(
            IReportSourceResolver parentResolver,
            IHttpContextAccessor httpContextAccessor,
            ReportConnectionResolver connectionResolver,
            ILogger<RegIdReportSourceResolver> logger)
        {
            _parentResolver = parentResolver;
            _httpContextAccessor = httpContextAccessor;
            _connectionResolver = connectionResolver;
            _logger = logger;
        }

        public ReportSource Resolve(
            string report,
            OperationOrigin operationOrigin,
            IDictionary<string, object> currentParameterValues)
        {
            var reportSource = _parentResolver.Resolve(report, operationOrigin, currentParameterValues);
            if (reportSource == null)
                return null!;

            var headers = _httpContextAccessor.HttpContext?.Request.Headers;
            var regId = _connectionResolver.ResolveRegId(headers, currentParameterValues);
            var connectionStringName = _connectionResolver.ResolveConnectionStringName(regId);
            var connectionInfo = _connectionResolver.DescribeConnection(headers, currentParameterValues);

            _logger.LogInformation(
                "Report {Report} using connection name {ConnectionName} -> {DataSource}/{Catalog} (RegId header: {HeaderRegId}, RegId used: {RegIdUsed})",
                report,
                connectionInfo.ConnectionStringName,
                connectionInfo.DataSource ?? "?",
                connectionInfo.InitialCatalog ?? "?",
                connectionInfo.RegIdFromHeader ?? "(none)",
                connectionInfo.RegIdUsed ?? "(none)");

            if (string.IsNullOrEmpty(connectionStringName))
                return reportSource;

            var connectionManager = new ReportConnectionStringManager(connectionStringName);
            return connectionManager.UpdateReportSource(reportSource);
        }
    }
}
