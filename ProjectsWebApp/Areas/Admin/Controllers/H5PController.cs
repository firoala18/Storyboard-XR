using System.IO.Compression;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProjectsWebApp.DataAccsess.Data;
using ProjectsWebApp.Models;

namespace ProjectsWebApp.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    public class H5PController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly IWebHostEnvironment _env;

        public H5PController(ApplicationDbContext db, IWebHostEnvironment env)
        {
            _db = db;
            _env = env;
        }

        // GET: /Admin/H5P
        public async Task<IActionResult> Index()
        {
            var items = await _db.H5PContents
                .OrderByDescending(h => h.CreatedAtUtc)
                .ToListAsync();
            return View(items);
        }

        // GET: /Admin/H5P/Create
        public IActionResult Create() => View(new H5PContent());

        // POST: /Admin/H5P/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            H5PContent model,
            IFormFile? h5pFile,
            IFormFile? imageFile)
        {
            ModelState.Remove(nameof(model.ContentPath));

            if (h5pFile == null || h5pFile.Length == 0)
                ModelState.AddModelError("h5pFile", "Bitte eine .h5p-Datei hochladen.");

            if (!ModelState.IsValid) return View(model);

            model.CreatedAtUtc = DateTime.UtcNow;
            model.IsPublished = false;
            model.OriginalFileName = h5pFile!.FileName;

            var contentId = Guid.NewGuid().ToString("N");
            var contentDir = Path.Combine(_env.WebRootPath, "uploads", "h5p", contentId);
            Directory.CreateDirectory(contentDir);

            using (var stream = h5pFile.OpenReadStream())
            using (var zip = new ZipArchive(stream, ZipArchiveMode.Read))
            {
                zip.ExtractToDirectory(contentDir, overwriteFiles: true);
            }
            model.ContentPath = $"/uploads/h5p/{contentId}";

            if (imageFile != null && imageFile.Length > 0)
            {
                var imgDir = Path.Combine(_env.WebRootPath, "uploads", "h5p", "images");
                Directory.CreateDirectory(imgDir);
                var imgName = $"{contentId}{Path.GetExtension(imageFile.FileName)}";
                var imgPath = Path.Combine(imgDir, imgName);
                await using (var fs = new FileStream(imgPath, FileMode.Create))
                    await imageFile.CopyToAsync(fs);
                model.ImagePath = $"/uploads/h5p/images/{imgName}";
            }

            _db.H5PContents.Add(model);
            await _db.SaveChangesAsync();

            TempData["success"] = "H5P-Inhalt wurde zur Sammlung hinzugefügt.";
            return RedirectToAction(nameof(Index));
        }

        // GET: /Admin/H5P/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            var item = await _db.H5PContents.FindAsync(id);
            if (item == null) return NotFound();
            return View(item);
        }

        // POST: /Admin/H5P/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int id,
            H5PContent model,
            IFormFile? h5pFile,
            IFormFile? imageFile)
        {
            var item = await _db.H5PContents.FindAsync(id);
            if (item == null) return NotFound();

            ModelState.Remove(nameof(model.ContentPath));
            if (!ModelState.IsValid) return View(model);

            item.Title = model.Title;
            item.Description = model.Description;
            item.Keywords = model.Keywords;
            item.UpdatedAtUtc = DateTime.UtcNow;

            if (h5pFile != null && h5pFile.Length > 0)
            {
                var oldDir = Path.Combine(_env.WebRootPath,
                    item.ContentPath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
                if (Directory.Exists(oldDir)) Directory.Delete(oldDir, recursive: true);

                var contentId = Guid.NewGuid().ToString("N");
                var contentDir = Path.Combine(_env.WebRootPath, "uploads", "h5p", contentId);
                Directory.CreateDirectory(contentDir);
                using (var stream = h5pFile.OpenReadStream())
                using (var zip = new ZipArchive(stream, ZipArchiveMode.Read))
                    zip.ExtractToDirectory(contentDir, overwriteFiles: true);

                item.ContentPath = $"/uploads/h5p/{contentId}";
                item.OriginalFileName = h5pFile.FileName;
            }

            if (imageFile != null && imageFile.Length > 0)
            {
                if (!string.IsNullOrEmpty(item.ImagePath))
                {
                    var oldImg = Path.Combine(_env.WebRootPath,
                        item.ImagePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
                    if (System.IO.File.Exists(oldImg)) System.IO.File.Delete(oldImg);
                }

                var imgDir = Path.Combine(_env.WebRootPath, "uploads", "h5p", "images");
                Directory.CreateDirectory(imgDir);
                var cId = Path.GetFileName(item.ContentPath);
                var imgName = $"{cId}{Path.GetExtension(imageFile.FileName)}";
                var imgPath = Path.Combine(imgDir, imgName);
                await using (var fs = new FileStream(imgPath, FileMode.Create))
                    await imageFile.CopyToAsync(fs);
                item.ImagePath = $"/uploads/h5p/images/{imgName}";
            }

            await _db.SaveChangesAsync();
            TempData["success"] = "H5P-Inhalt aktualisiert.";
            return RedirectToAction(nameof(Index));
        }

        // POST: /Admin/H5P/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var item = await _db.H5PContents.FindAsync(id);
            if (item == null) return NotFound();

            var contentDir = Path.Combine(_env.WebRootPath,
                item.ContentPath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
            if (Directory.Exists(contentDir)) Directory.Delete(contentDir, recursive: true);

            if (!string.IsNullOrEmpty(item.ImagePath))
            {
                var imgPath = Path.Combine(_env.WebRootPath,
                    item.ImagePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
                if (System.IO.File.Exists(imgPath)) System.IO.File.Delete(imgPath);
            }

            _db.H5PContents.Remove(item);
            await _db.SaveChangesAsync();

            TempData["success"] = "H5P-Inhalt gelöscht.";
            return RedirectToAction(nameof(Index));
        }

        // POST: /Admin/H5P/TogglePublish/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> TogglePublish(int id)
        {
            var item = await _db.H5PContents.FindAsync(id);
            if (item == null) return NotFound();
            item.IsPublished = !item.IsPublished;
            item.UpdatedAtUtc = DateTime.UtcNow;
            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
    }
}
