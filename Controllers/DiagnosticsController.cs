using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using apireport.Extensions;

namespace apireport.Controllers
{
    /// <summary>
    /// Safe diagnostics for tenant DB routing (no passwords returned).
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    [AllowAnonymous]
    public class DiagnosticsController : ControllerBase
    {
        [HttpGet("report-connection")]
        public ActionResult<ReportConnectionInfo> GetReportConnection(
            [FromQuery] string? regId,
            [FromServices] ReportConnectionResolver resolver)
        {
            var info = resolver.DescribeConnection(Request.Headers, null, regId);
            return Ok(info);
        }
    }
}
