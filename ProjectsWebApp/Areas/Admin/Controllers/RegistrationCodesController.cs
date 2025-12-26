using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProjectsWebApp.DataAccsess.Data;
using ProjectsWebApp.Models;

namespace ProjectsWebApp.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    public class RegistrationCodesController : Controller
    {
        private readonly ApplicationDbContext _db;
        public RegistrationCodesController(ApplicationDbContext db) => _db = db;

        public async Task<IActionResult> Index()
        {
            var codes = await _db.RegistrationCodes
                                 .OrderBy(c => c.Id)
                                 .ToListAsync();
            return View(codes);
        }

        public async Task<IActionResult> Upsert(int? id)
        {
            if (id == null)
                return View(new RegistrationCode());

            var code = await _db.RegistrationCodes.FindAsync(id);
            if (code == null) return NotFound();
            return View(code);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Upsert(RegistrationCode model)
        {
            if (!ModelState.IsValid)
                return View(model);

            if (model.Id == 0)
            {
                _db.RegistrationCodes.Add(model);
                TempData["success"] = "Code angelegt.";
            }
            else
            {
                var ent = await _db.RegistrationCodes.FindAsync(model.Id);
                if (ent == null) return NotFound();
                ent.Code = model.Code;
                ent.IsActive = model.IsActive;
                ent.Note = model.Note;
                TempData["success"] = "Code aktualisiert.";
            }
            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var ent = await _db.RegistrationCodes.FindAsync(id);
            if (ent != null)
            {
                _db.RegistrationCodes.Remove(ent);
                await _db.SaveChangesAsync();
                TempData["success"] = "Code gelöscht.";
            }
            return RedirectToAction(nameof(Index));
        }

        /// <summary>
        /// Aktive/​Inaktive umschalten
        /// </summary>
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Toggle(int id)
        {
            var ent = await _db.RegistrationCodes.FindAsync(id);
            if (ent != null)
            {
                ent.IsActive = !ent.IsActive;
                await _db.SaveChangesAsync();
                TempData["success"] = ent.IsActive
                    ? "Code aktiviert."
                    : "Code deaktiviert.";
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
