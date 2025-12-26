using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProjectsWebApp.DataAccsess.Data;
using ProjectsWebApp.Models;
using System.Linq;
using System.Threading.Tasks;

namespace ProjectsWebApp.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class LockedAccountsController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<IdentityUser> _userManager;

        public LockedAccountsController(ApplicationDbContext db, UserManager<IdentityUser> userManager)
        {
            _db = db;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var query = from s in _db.UserSecurityStates
                        join u in _db.Users on s.UserId equals u.Id
                        where s.IsManuallyLocked
                        orderby u.Email
                        select new LockedAccountViewModel
                        {
                            UserId = u.Id,
                            Email = u.Email,
                            LockoutCount = s.LockoutCount,
                            LockoutEnd = u.LockoutEnd
                        };

            var model = await query.ToListAsync();
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Unlock(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return BadRequest();

            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            var state = await _db.UserSecurityStates.SingleOrDefaultAsync(s => s.UserId == id);
            if (state != null)
            {
                state.IsManuallyLocked = false;
                state.LockoutCount = 0;
                state.LastLockoutUtc = null;
            }

            user.LockoutEnd = null;
            user.AccessFailedCount = 0;
            await _userManager.UpdateAsync(user);
            await _db.SaveChangesAsync();

            TempData["success"] = "Konto wurde reaktiviert.";
            return RedirectToAction(nameof(Index));
        }
    }
}
