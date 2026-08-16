using System;
using System.IO;
using System.Linq;
using Telerik.Reporting;
using Telerik.Reporting.Services;
using Telerik.Reporting.XmlSerialization;

namespace apireport.Extensions
{
    /// <summary>
    /// Swaps SqlDataSource connection names on a report graph before rendering.
    /// Reports are authored with <c>Reporting.AHHA</c>; runtime replaces it with the tenant key
    /// from regCompany.json (e.g. DbConnection, DBConnection_Live).
    /// Pattern: Telerik KB — changing connection string dynamically in a REST service resolver.
    /// </summary>
    public sealed class ReportConnectionStringManager
    {
        private readonly string _connectionStringName;

        public ReportConnectionStringManager(string connectionStringName)
        {
            if (string.IsNullOrWhiteSpace(connectionStringName))
                throw new ArgumentException("Connection string name is required.", nameof(connectionStringName));

            _connectionStringName = connectionStringName;
        }

        public ReportSource UpdateReportSource(ReportSource sourceReportSource)
        {
            switch (sourceReportSource)
            {
                case UriReportSource uriReportSource:
                {
                    ValidateReportSource(uriReportSource.Uri);
                    var reportInstance = UnpackageReport(uriReportSource);
                    SetConnectionStringName(reportInstance);
                    return CreateInstanceReportSource(reportInstance, uriReportSource);
                }
                case XmlReportSource xmlReportSource:
                {
                    ValidateReportSource(xmlReportSource.Xml);
                    var reportInstance = DeserializeReport(xmlReportSource);
                    SetConnectionStringName(reportInstance);
                    return CreateInstanceReportSource(reportInstance, xmlReportSource);
                }
                case InstanceReportSource instanceReportSource:
                {
                    if (instanceReportSource.ReportDocument is ReportItemBase reportItemBase)
                        SetConnectionStringName(reportItemBase);
                    return instanceReportSource;
                }
                case TypeReportSource typeReportSource:
                {
                    ValidateReportSource(typeReportSource.TypeName);
                    var reportType = Type.GetType(typeReportSource.TypeName);
                    if (reportType == null)
                        return sourceReportSource;

                    var reportInstance = (Report)Activator.CreateInstance(reportType)!;
                    SetConnectionStringName(reportInstance);
                    return CreateInstanceReportSource(reportInstance, typeReportSource);
                }
                default:
                    return sourceReportSource;
            }
        }

        private static ReportSource CreateInstanceReportSource(
            IReportDocument report,
            ReportSource originalReportSource)
        {
            var instanceReportSource = new InstanceReportSource { ReportDocument = report };
            instanceReportSource.Parameters.AddRange(originalReportSource.Parameters);
            return instanceReportSource;
        }

        private static void ValidateReportSource(string value)
        {
            if (value.TrimStart().StartsWith('='))
            {
                throw new InvalidOperationException(
                    "Expressions for ReportSource are not supported when changing the connection string dynamically.");
            }
        }

        private static Report UnpackageReport(UriReportSource uriReportSource)
        {
            var reportPackager = new ReportPackager();
            using var sourceStream = File.OpenRead(uriReportSource.Uri);
            return (Report)reportPackager.UnpackageDocument(sourceStream);
        }

        private static Report DeserializeReport(XmlReportSource xmlReportSource)
        {
            var settings = new System.Xml.XmlReaderSettings { IgnoreWhitespace = true };
            using var textReader = new StringReader(xmlReportSource.Xml);
            using var xmlReader = System.Xml.XmlReader.Create(textReader, settings);
            var xmlSerializer = new ReportXmlSerializer();
            return (Report)xmlSerializer.Deserialize(xmlReader)!;
        }

        private void SetConnectionStringName(ReportItemBase reportItemBase)
        {
            if (reportItemBase is Report report)
            {
                if (report.DataSource is SqlDataSource reportDataSource)
                    reportDataSource.ConnectionString = _connectionStringName;

                foreach (var parameter in report.ReportParameters)
                {
                    if (parameter.AvailableValues.DataSource is SqlDataSource parameterDataSource)
                        parameterDataSource.ConnectionString = _connectionStringName;
                }
            }

            foreach (var item in reportItemBase.Items)
            {
                SetConnectionStringName(item);

                // Drill-through targets are resolved separately by the report service.
                if (item.Action is NavigateToReportAction)
                    continue;

                if (item is SubReport subReport && subReport.ReportSource != null)
                {
                    subReport.ReportSource = UpdateReportSource(subReport.ReportSource);
                    continue;
                }

                if (item is DataItem dataItem && dataItem.DataSource is SqlDataSource itemDataSource)
                    itemDataSource.ConnectionString = _connectionStringName;
            }

            if (reportItemBase is Report reportForSources)
            {
                foreach (var sqlDataSource in reportForSources.GetDataSources().OfType<SqlDataSource>())
                    sqlDataSource.ConnectionString = _connectionStringName;
            }
        }
    }
}
