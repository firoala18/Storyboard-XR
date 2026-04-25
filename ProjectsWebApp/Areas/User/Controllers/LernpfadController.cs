using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProjectsWebApp.DataAccsess.Data;
using System.Security.Claims;

namespace ProjectsWebApp.Areas.User.Controllers
{
    [Authorize]
    [Area("User")]
    public class LernpfadController : Controller
    {
        private readonly ApplicationDbContext _context;
        public LernpfadController(ApplicationDbContext context)
        {
            _context = context;
        }

        private string? CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier);

        [HttpGet]
        public async Task<IActionResult> Index([FromQuery] string? q)
        {
            var uid = CurrentUserId;
            if (string.IsNullOrWhiteSpace(uid)) return Challenge();

            var flowsQ = _context.LernFlows
                .Where(f => f.OwnerId == uid)
                .Include(f => f.Steps)
                .AsNoTracking();

            if (!string.IsNullOrWhiteSpace(q))
            {
                var needle = q.Trim().ToLowerInvariant();
                flowsQ = flowsQ.Where(f => f.Title.ToLower().Contains(needle) || (f.Description ?? "").ToLower().Contains(needle));
                ViewBag.Query = q;
            }

            var list = await flowsQ
                .OrderByDescending(f => f.CreatedAt).ThenByDescending(f => f.Id)
                .ToListAsync();

            // Reuse public Index view (works for both).
            return View("~/Areas/User/Views/Lernpfad/Index.cshtml", list);
        }

        // Convenience: redirect to public slug view
        [HttpGet("open/{slug}")]
        public IActionResult Open(string slug)
        {
            return RedirectToAction("ViewBySlug", "PublicLernpfad", new { area = "User", slug });
        }
    }
}
