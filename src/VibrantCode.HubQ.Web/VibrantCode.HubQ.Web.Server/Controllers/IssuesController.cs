using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace VibrantCode.HubQ.Web.Server.Controllers
{
    [ApiController]
    [Route("/api/issues")]
    public class IssuesController : Controller
    {
        [HttpGet]
        public async Task<ActionResult> GetAsync()
        {
            
        }
    }
}
