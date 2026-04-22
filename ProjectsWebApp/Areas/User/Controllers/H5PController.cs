using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProjectsWebApp.DataAccsess.Data;

namespace ProjectsWebApp.Areas.User.Controllers
{
    [Area("User")]
    [Authorize]
    public class H5PController : Controller
    {
        private readonly ApplicationDbContext _db;
        public H5PController(ApplicationDbContext db) => _db = db;

        // GET: /User/H5P/Gallery?search=...
        public async Task<IActionResult> Gallery(string? search = null)
        {
            var query = _db.H5PContents.Where(h => h.IsPublished).AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim().ToLower();
                query = query.Where(h =>
                    h.Title.ToLower().Contains(s) ||
                    (h.Description != null && h.Description.ToLower().Contains(s)) ||
                    (h.Keywords != null && h.Keywords.ToLower().Contains(s)));
            }

            var items = await query.OrderByDescending(h => h.CreatedAtUtc).ToListAsync();
            ViewBag.Search = search;
            return View(items);
        }

        // GET: /User/H5P/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var item = await _db.H5PContents.FindAsync(id);
            if (item == null || !item.IsPublished) return NotFound();
            return View(item);
        }
    }
}
