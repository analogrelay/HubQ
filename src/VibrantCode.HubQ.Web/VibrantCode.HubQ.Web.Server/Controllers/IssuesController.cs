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
        public async Task<ActionResult<IEnumerable<PagedResponse<IssueResponse>>>> GetAsync(int pageNumber = 1, int pageSize = 25)
        {

            return Json(new PagedResponse<IssueResponse>()
            {
                Links = Hyperlinks.GeneratePagingLinks(o => Url.Action("Get", "Issues", o), pageNumber, pageSize),
                Data = await _db.Issues
                    .OrderByDescending(i => i.UpdatedAt)
                    .Where(i => !i.IsPullRequest)
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .Select(i => new IssueResponse()
                    {
                        Id = i.Id,
                        Repository = $"{i.Repository!.Owner}/{i.Repository!.Name}",
                        Number = i.Number,
                        Title = i.Title!,
                    }).ToListAsync()
            });
        }
    }
}
