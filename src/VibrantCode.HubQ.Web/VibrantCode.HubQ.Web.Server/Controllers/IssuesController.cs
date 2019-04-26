using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using VibrantCode.HubQ.Data;
using VibrantCode.HubQ.Web.Models;

namespace VibrantCode.HubQ.Web.Server.Controllers
{
    [ApiController]
    [Route("/api/issues")]
    public class IssuesController : Controller
    {
        private readonly HubSyncDbContext _db;

        public IssuesController(HubSyncDbContext db)
        {
            _db = db;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<IssueResponse>>> GetAsync()
        {
            return await _db.Issues
                .Take(10)
                .Select(i => new IssueResponse()
                {
                    Id = i.Id,
                    Repository = $"{i.Repository!.Owner}/{i.Repository!.Name}",
                    Number = i.Number,
                    Title = i.Title!,
                }).ToListAsync();
        }
    }
}
