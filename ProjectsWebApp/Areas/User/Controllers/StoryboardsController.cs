using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProjectsWebApp.DataAccsess.Data;
using ProjectsWebApp.Models;
using ProjectsWebApp.Utility;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace ProjectsWebApp.Areas.User.Controllers
{
    [Area("User")]
    [Authorize] // default: signed-in users; we will open specific endpoints with [AllowAnonymous]
    public class StoryboardsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _env;
        private readonly UserManager<IdentityUser> _userManager;

        private const long MaxImageSizeBytes = 2 * 1024 * 1024;

        public StoryboardsController(
            ApplicationDbContext context,
            IWebHostEnvironment env,
            UserManager<IdentityUser> userManager)
        {
            _context = context;
            _env = env;
            _userManager = userManager;
        }

        private string? CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier);
        private bool IsSuper => User.IsInRole("SuperAdmin");

        // ---- Token helper for guest access ----
        private static string NewToken()
        {
            // URL-safe ~22 chars
            var b = RandomNumberGenerator.GetBytes(16);
            return Convert.ToBase64String(b).TrimEnd('=').Replace('+', '-').Replace('/', '_');
        }

        // GET: /User/Storyboards[?uid=<userId>]
        // My boards by default. SuperAdmin can browse another user's boards via uid.
        public async Task<IActionResult> Index(string? uid = null)
        {
            var targetUserId = CurrentUserId!;
            string? viewedUserEmail = null;

            if (!string.IsNullOrWhiteSpace(uid) && IsSuper)
            {
                var u = await _userManager.FindByIdAsync(uid);
                if (u != null)
                {
                    targetUserId = uid;
                    viewedUserEmail = u.Email;
                }
            }

            var items = await _context.Storyboards
                .Where(s => s.OwnerId == targetUserId)
                .OrderByDescending(s => s.CreatedAt)
                .ThenByDescending(s => s.Id)
                .Include(s => s.Scenes)
                .AsNoTracking()
                .ToListAsync();

            ViewBag.ImpersonatedEmail = viewedUserEmail;
            ViewBag.ImpersonatedUserId = viewedUserEmail != null ? targetUserId : null;

            return View(items);
        }

        // GET: /User/Storyboards/Details/5?sceneId=12[&t=EDIT_TOKEN]
        // Open edit view. Allow owner/super, or anonymous with a valid EDIT token.
        [AllowAnonymous]
        public async Task<IActionResult> Details(int id, int? sceneId, string? t)
        {
            var sb = await _context.Storyboards
                .Include(s => s.Scenes)
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.Id == id);

            if (sb == null) return NotFound();

            var isOwner = (sb.OwnerId != null && sb.OwnerId == CurrentUserId)
                          || (!string.IsNullOrWhiteSpace(sb.OwnerTokenHash) && GetOrCreateOwnerTokenHash(HttpContext) == sb.OwnerTokenHash);
            var hasAnyToken = !string.IsNullOrWhiteSpace(t) && (t == sb.AccessTokenView || t == sb.AccessTokenEdit);
            var hasEditToken = !string.IsNullOrWhiteSpace(t) && t == sb.AccessTokenEdit;

            if (!isOwner && !IsSuper && !hasAnyToken)
                return Forbid();

            int? activeSceneId = sceneId;
            if (activeSceneId.HasValue && !sb.Scenes.Any(sc => sc.Id == activeSceneId.Value))
                activeSceneId = null;

            if (!activeSceneId.HasValue)
                activeSceneId = sb.Scenes
                    .OrderBy(sc => sc.Number)
                    .ThenBy(sc => sc.Id)
                    .Select(sc => (int?)sc.Id)
                    .FirstOrDefault();

            ViewBag.ActiveSceneId = activeSceneId;
            ViewBag.Token = t; // pass through so client JS can call APIs with ?token=
            ViewBag.IsGuestEditor = !isOwner && !IsSuper && hasEditToken;

            var isStaff = User.IsInRole("Admin") || IsSuper;
            ViewBag.IsStaff = isStaff;

            // read-only in the Details view if user cannot edit (no owner, no super, no edit token)
            var canEdit = isOwner || IsSuper || hasEditToken;
            ViewData["ReadOnly"] = !canEdit;

            // AI-Bildgenerierung NUR für authentifizierte Nutzer,
            // die entweder bearbeiten dürfen oder Staff (Admin/Super) sind.
            var isAuthenticated = User?.Identity?.IsAuthenticated ?? false;
            ViewBag.CanGenerateAi = isAuthenticated && (canEdit || isStaff);

            return View(sb);
        }

        // GET: /User/Storyboards/Create  (signed-in flow)
        public IActionResult Create() => View();

        // POST: /User/Storyboards/Create  (signed-in flow)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            [FromForm] string title,
            [FromForm] IFormFile image,
            [FromForm] string? zielgruppe,
            [FromForm] string? beschreibung,
            [FromForm] string? lernziel,
            [FromForm] string? farbpalette,
            [FromForm] TaxonomieStufe? taxonomie,
            [FromForm] LicenseType? license,
            [FromForm] string? authors,
            [FromForm] string? licenseExtras
        )
        {
            if (string.IsNullOrWhiteSpace(title))
                ModelState.AddModelError("title", "Title is required");
            if (image == null || image.Length == 0)
                ModelState.AddModelError("image", "Cover-Bild ist erforderlich");
            if (image != null && image.Length > MaxImageSizeBytes)
                ModelState.AddModelError("image", "Bild darf maximal 2 MB groß sein.");
            if (!ModelState.IsValid) return View();

            // --- save image ---
            var webRoot = _env.WebRootPath ?? Path.Combine(_env.ContentRootPath, "wwwroot");
            var uploadsDir = Path.Combine(webRoot, "uploads");
            Directory.CreateDirectory(uploadsDir);

            var ext = Path.GetExtension(image.FileName).ToLowerInvariant();
            var allowed = new[] { ".png", ".jpg", ".jpeg", ".webp", ".gif" };
            if (!allowed.Contains(ext))
            {
                ModelState.AddModelError("image", "Only PNG/JPG/WebP/GIF are allowed");
                return View();
            }

            var fileName = $"{Guid.NewGuid():N}{ext}";
            await using (var fs = new FileStream(Path.Combine(uploadsDir, fileName), FileMode.Create))
                await image.CopyToAsync(fs);

            // --- link-based access fields (REQUIRED) ---
            var publicId = await NextPublicIdAsync();     // short slug for /s/{slug}
            var editKeyPlain = UrlSafeRandom(24);         // show this to the user once if you want
            var editKeyHash = Sha256Hex(editKeyPlain);

            // if user is anonymous, bind to cookie token hash; otherwise keep OwnerId
            var ownerTokenHash = string.IsNullOrEmpty(CurrentUserId) ? GetOrCreateOwnerTokenHash(HttpContext) : null;

            // --- create board ---
            var sb = new Storyboard
            {
                Title = title.Trim(),
                Zielgruppe = (zielgruppe ?? "").Trim(),
                Beschreibung = (beschreibung ?? "").Trim(),
                Lernziel = (lernziel ?? "").Trim(),
                Farbpalette = NormalizePalette(farbpalette),
                Taxonomie = taxonomie,
                License = license,
                Authors = NormalizeAuthors(authors),
                LicenseExtras = string.IsNullOrWhiteSpace(licenseExtras) ? null : licenseExtras.Trim(),
                OwnerId = CurrentUserId,          // null when not logged in
                OwnerTokenHash = ownerTokenHash,       // set for anonymous owners
                PublicId = publicId,
                EditKeyHash = editKeyHash,
                CreatedAt = DateTime.UtcNow,
                LastSeenAt = DateTime.UtcNow,
                // Save cover path to both new and legacy fields for backward compatibility
                CoverImagePath = "/uploads/" + fileName,
                ImagePath = "/uploads/" + fileName // legacy
            };

            _context.Storyboards.Add(sb);
            await _context.SaveChangesAsync();

            // optional: surface the one-time edit key to the user
            TempData["Info"] = $"Öffentlicher Link: /s/{publicId}  •  Edit-Key: {editKeyPlain}";

            return RedirectToAction(nameof(Details), new { id = sb.Id, sceneId = (int?)null });
        }

        // ---------- helpers ----------

        private async Task<string> NextPublicIdAsync()
        {
            string slug;
            do { slug = UrlSafeRandom(8); }
            while (await _context.Storyboards.AnyAsync(s => s.PublicId == slug));
            return slug;
        }

        private static string UrlSafeRandom(int length)
        {
            // URL-safe base64 without padding, then trim to length
            var bytes = RandomNumberGenerator.GetBytes(length);
            var s = Convert.ToBase64String(bytes).Replace('+', '-').Replace('/', '_').TrimEnd('=');
            return s.Length >= length ? s[..length] : s;
        }

        private static string Sha256Hex(string s)
        {
            var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(s));
            return Convert.ToHexString(bytes).ToLowerInvariant();
        }

        private static string GetOrCreateOwnerTokenHash(HttpContext ctx)
        {
            const string cookieName = "sb_uid";
            var tok = ctx.Request.Cookies[cookieName];
            if (string.IsNullOrWhiteSpace(tok))
            {
                tok = UrlSafeRandom(32);
                ctx.Response.Cookies.Append(cookieName, tok, new CookieOptions
                {
                    HttpOnly = true,
                    SameSite = SameSiteMode.Lax,
                    Secure = ctx.Request.IsHttps,
                    Expires = DateTimeOffset.UtcNow.AddYears(1)
                });
            }
            return Sha256Hex(tok);
        }

        // Normalize a free-form list of authors into a comma-separated single string
        public static string? NormalizeAuthors(string? raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return null;
            var items = raw
                .Replace("\n", ",")
                .Replace("\r", ",")
                .Replace(";", ",")
                .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(s => s.Trim())
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();
            return items.Length == 0 ? null : string.Join(", ", items);
        }

        // Normalize a free-form list of colors into a comma-separated single string
        public static string NormalizePalette(string? raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return "";
            var parts = raw
                .Replace(";", ",")
                .Replace("\n", ",")
                .Replace("\r", ",")
                .Split(new[] { ',', ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(s => s.Trim())
                .Select(s => s.StartsWith('#') ? s : "#" + s)
                .Select(s => s.Length == 4 || s.Length == 7 ? s.ToLowerInvariant() : s.ToLowerInvariant())
                .Distinct();
            return string.Join(",", parts);
        }

        // -------- Guest/Anonymous creation flow --------

        // GET: /User/Storyboards/CreateGuest (anonymous-friendly)
        [AllowAnonymous]
        public IActionResult CreateGuest()
        {
            TempData["Info"] = "Die anonyme Erstellung wurde in den öffentlichen Flow verschoben.";
            return RedirectToAction("Create", "PublicStoryboards", new { area = "User" });
        }

        // POST: /User/Storyboards/CreateGuest (anonymous-friendly)
        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CreateGuest(
            [FromForm] string title,
            [FromForm] IFormFile image,
            [FromForm] string? zielgruppe,
            [FromForm] string? beschreibung,
            [FromForm] string? lernziel,
            [FromForm] string? farbpalette,
            [FromForm] string? sceneName,
            [FromForm] int? sceneNumber)
        {
            TempData["Info"] = "Bitte nutzen Sie den öffentlichen Erstellungs-Flow.";
            return RedirectToAction("Create", "PublicStoryboards", new { area = "User" });
        }

        // GET: /User/Storyboards/Edit/5[?t=EDIT_TOKEN]
        // Allow owner/super, or anonymous with valid EDIT token.
        [AllowAnonymous]
        public async Task<IActionResult> Edit(int id, string? t)
        {
            var sb = await _context.Storyboards
                .Include(s => s.Scenes)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (sb == null) return NotFound();

            var isOwner = sb.OwnerId != null && sb.OwnerId == CurrentUserId;
            var hasEditToken = !string.IsNullOrWhiteSpace(t) && t == sb.AccessTokenEdit;
            if (!isOwner && !IsSuper && !hasEditToken) return Forbid();

            // ⬇️ add this block
            var firstScene = sb.Scenes
                .OrderBy(sc => sc.Number)
                .ThenBy(sc => sc.Id)
                .FirstOrDefault();
            ViewBag.FirstSceneId = firstScene?.Id;
            ViewBag.FirstSceneNumber = firstScene?.Number ?? 1;
            ViewBag.FirstSceneName = firstScene?.Name;

            var cover = !string.IsNullOrWhiteSpace(sb.ImagePath)
                ? sb.ImagePath
                : sb.Scenes?
                    .OrderBy(sc => sc.Number)
                    .ThenBy(sc => sc.Id)
                    .Select(sc => sc.ImagePath)
                    .FirstOrDefault();

            ViewBag.CoverPath = cover;
            ViewBag.Token = t;
            ViewBag.IsGuestEditor = !isOwner && !IsSuper && hasEditToken;

            return View(sb);
        }


        // POST: /User/Storyboards/Edit/5[?t=EDIT_TOKEN]
        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
     int id,
     [Bind("Id,Title,Zielgruppe,Beschreibung,Lernziel,Farbpalette,Taxonomie,License,Authors,LicenseExtras")] Storyboard input,
     IFormFile? image,
     string? t)
        {
            var sb = await _context.Storyboards
                .Include(s => s.Scenes)  // ⬅️ ensure scenes are loaded
                .FirstOrDefaultAsync(s => s.Id == id);
            if (sb == null) return NotFound();

            var isOwner = sb.OwnerId != null && sb.OwnerId == CurrentUserId;
            var hasEditToken = !string.IsNullOrWhiteSpace(t) && t == sb.AccessTokenEdit;
            if (!isOwner && !IsSuper && !hasEditToken) return Forbid();

            if (string.IsNullOrWhiteSpace(input.Title))
            {
                ModelState.AddModelError("Title", "Title is required");
                return View(sb);
            }

            // ----- update storyboard fields -----
            sb.Title = input.Title.Trim();
            sb.Zielgruppe = (input.Zielgruppe ?? "").Trim();
            sb.Beschreibung = (input.Beschreibung ?? "").Trim();
            sb.Lernziel = (input.Lernziel ?? "").Trim();
            sb.Farbpalette = NormalizePalette(input.Farbpalette);
            sb.Taxonomie = input.Taxonomie;
            sb.License = input.License;
            sb.Authors = NormalizeAuthors(input.Authors);
            sb.LicenseExtras = string.IsNullOrWhiteSpace(input.LicenseExtras) ? null : input.LicenseExtras.Trim();

            // ----- optional: replace COVER image (not scene) -----
            if (image != null && image.Length > 0)
            {
                if (image.Length > MaxImageSizeBytes)
                {
                    ModelState.AddModelError("image", "Bild darf maximal 2 MB groß sein.");
                    return View(sb);
                }

                var ext = Path.GetExtension(image.FileName).ToLowerInvariant();
                var allowed = new[] { ".png", ".jpg", ".jpeg", ".webp", ".gif" };
                if (!allowed.Contains(ext))
                {
                    ModelState.AddModelError("image", "Only PNG/JPG/WebP/GIF are allowed");
                    return View(sb);
                }

                var webRoot = _env.WebRootPath ?? Path.Combine(_env.ContentRootPath, "wwwroot");
                var uploadsDir = Path.Combine(webRoot, "uploads");
                Directory.CreateDirectory(uploadsDir);

                var fileName = $"{Guid.NewGuid():N}{ext}";
                var dest = Path.Combine(uploadsDir, fileName);
                await using (var fs = new FileStream(dest, FileMode.Create))
                    await image.CopyToAsync(fs);

                // delete old cover file if present
                var existingCover = !string.IsNullOrWhiteSpace(sb.CoverImagePath) ? sb.CoverImagePath : sb.ImagePath;
                if (!string.IsNullOrWhiteSpace(existingCover))
                {
                    var oldPath = Path.Combine(webRoot, existingCover.TrimStart('/'));
                    if (System.IO.File.Exists(oldPath)) System.IO.File.Delete(oldPath);
                }

                var newPath = "/uploads/" + fileName;
                sb.CoverImagePath = newPath;
                sb.ImagePath = newPath; // legacy sync
            }

            await _context.SaveChangesAsync();

            // keep token if present
            return RedirectToAction(nameof(Details), new { id = sb.Id, t, sceneId = (int?)null });
        }

        // POST: /User/Storyboards/Delete/5   (only owner/super)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var sb = await _context.Storyboards
                .Include(s => s.Scenes)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (sb == null) return NotFound();
            if (sb.OwnerId != CurrentUserId && !IsSuper) return Forbid();

            // remove scene image files
            var webRoot = _env.WebRootPath;
            if (string.IsNullOrEmpty(webRoot))
                webRoot = Path.Combine(_env.ContentRootPath, "wwwroot");

            foreach (var sc in sb.Scenes)
            {
                if (!string.IsNullOrWhiteSpace(sc.ImagePath))
                {
                    var path = Path.Combine(webRoot, sc.ImagePath.TrimStart('/'));
                    if (System.IO.File.Exists(path)) System.IO.File.Delete(path);
                }
            }

            // remove cover file (new field preferred) and legacy
            var coverPath = !string.IsNullOrWhiteSpace(sb.CoverImagePath) ? sb.CoverImagePath : sb.ImagePath;
            if (!string.IsNullOrWhiteSpace(coverPath))
            {
                var cover = Path.Combine(webRoot, coverPath.TrimStart('/'));
                if (System.IO.File.Exists(cover)) System.IO.File.Delete(cover);
            }

            _context.Storyboards.Remove(sb);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // GET: /User/Storyboards/Viewer/5?sceneId=12[&t=VIEW_OR_EDIT_TOKEN]
        // Read-only viewer. Allow owner/super, or anonymous with a valid VIEW or EDIT token.
        [AllowAnonymous]
        [HttpGet]
        public async Task<IActionResult> Viewer(int id, int? sceneId, string? t)
        {
            var sb = await _context.Storyboards
                .Include(s => s.Scenes)
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.Id == id);

            if (sb == null) return NotFound();

            var isOwner = sb.OwnerId != null && sb.OwnerId == CurrentUserId;
            var hasAnyToken = !string.IsNullOrWhiteSpace(t) && (t == sb.AccessTokenView || t == sb.AccessTokenEdit);

            if (!isOwner && !IsSuper && !hasAnyToken)
                return Forbid();

            // choose active scene
            int? activeSceneId = sceneId;
            if (activeSceneId.HasValue && !(sb.Scenes?.Any(sc => sc.Id == activeSceneId.Value) ?? false))
                activeSceneId = null;

            if (!activeSceneId.HasValue)
                activeSceneId = sb.Scenes?
                    .OrderBy(sc => sc.Number)
                    .ThenBy(sc => sc.Id)
                    .Select(sc => (int?)sc.Id)
                    .FirstOrDefault();

            ViewBag.ActiveSceneId = activeSceneId;
            ViewBag.Token = t; // so the view can pass it to /api endpoints
            return View(sb);
        }

        // GET: /User/Storyboards/PromptList/5[?t=EDIT_TOKEN]
        // Read-only list of all Prompt-Idee texts grouped by scene.
        // Allow owner/super, or anonymous with a valid VIEW or EDIT token (same as Viewer).
        [AllowAnonymous]
        [HttpGet]
        public async Task<IActionResult> PromptList(int id, string? t)
        {
            var sb = await _context.Storyboards
                .Include(s => s.Scenes)
                    .ThenInclude(sc => sc.Markers)
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.Id == id);

            if (sb == null) return NotFound();

            ViewBag.Token = t;
            return View(sb);
        }

        // GET: /User/Storyboards/ReflexionList/5[?t=EDIT_TOKEN]
        // Read-only list of all Reflexion – Notizen texts grouped by scene.
        // Allow owner/super, or anonymous with a valid VIEW or EDIT token (same as Viewer).
        [AllowAnonymous]
        [HttpGet]
        public async Task<IActionResult> ReflexionList(int id, string? t)
        {
            var sb = await _context.Storyboards
                .Include(s => s.Scenes)
                    .ThenInclude(sc => sc.Markers)
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.Id == id);

            if (sb == null) return NotFound();

            ViewBag.Token = t;
            return View(sb);
        }
    }
}
