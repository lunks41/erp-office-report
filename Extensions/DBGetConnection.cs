using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace apireport.Extensions
{
    public class CompanyRegistration
    {
        public string RegId { get; set; } = string.Empty;
        public string CompanyName { get; set; } = string.Empty;
        public string ConnectionStringName { get; set; } = string.Empty;
    }

    public class DBGetConnection
    {
        private readonly string _regCompanyPath;

        public DBGetConnection(string contentRootPath)
        {
            _regCompanyPath = Path.Combine(contentRootPath, "regCompany.json");
        }

        public string? GetconnectionDB(string RegId)
        {
            var regCompany = LoadRegistrations();
            return regCompany?.FirstOrDefault(b => b.RegId == RegId)?.ConnectionStringName;
        }

        public bool ValidateRegId(string RegId)
        {
            return !string.IsNullOrEmpty(GetconnectionDB(RegId));
        }

        private IEnumerable<CompanyRegistration>? LoadRegistrations()
        {
            if (!File.Exists(_regCompanyPath))
                return null;

            var regCompanyData = File.ReadAllText(_regCompanyPath);
            return JsonConvert.DeserializeObject<IEnumerable<CompanyRegistration>>(regCompanyData);
        }
    }
}
