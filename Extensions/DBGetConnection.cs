using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace erpofficereport.Extensions
{
    public class CompanyRegistration
    {
        public string RegId { get; set; }
        public string CompanyName { get; set; }
        public string ConnectionStringName { get; set; }
    }
    public class DBGetConnection
    {
        public string GetconnectionDB(string RegId)
        {
            //read the company registration data from json
            string regCompanyData = File.ReadAllText("regCompany.json");

            //Convert json to object list
            var regCompany = JsonConvert.DeserializeObject<IEnumerable<CompanyRegistration>>(regCompanyData);

            // find out the RegId & get the connectionstring from there
            return regCompany.Where(b => b.RegId == RegId).FirstOrDefault().ConnectionStringName;
        }

        public bool ValidateRegId(string RegId)
        {
            //read the company registration data from json
            string regCompanyData = File.ReadAllText("regCompany.json");

            //Convert json to object list
            var regCompany = JsonConvert.DeserializeObject<IEnumerable<CompanyRegistration>>(regCompanyData);

            // find out the RegId & get the connectionstring from there
            var CheckData = regCompany.Where(b => b.RegId == RegId).FirstOrDefault().ConnectionStringName;

            return (CheckData != null ? true : false);
        }
    }
}