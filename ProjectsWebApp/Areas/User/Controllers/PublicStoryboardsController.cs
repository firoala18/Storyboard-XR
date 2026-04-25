using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProjectsWebApp.DataAccsess.Data;
using ProjectsWebApp.Models;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace ProjectsWebApp.Areas.User.Controllers
{
    [AllowAnonymous]
    [Area("User")]
    [Route("s")]
    public class PublicStoryboardsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _env;

        private const long MaxImageSizeBytes = 2 * 1024 * 1024;

        public PublicStoryboardsController(ApplicationDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        // ---------- constants / helpers ----------
        private const string AnonCookie = "sb_uid";
        private string? CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier);

        private static string Sha256Hex(string value)
        {
            var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(value));
            return Convert.ToHexString(bytes).ToLowerInvariant();
        }

        private static string UrlSafeRandom(int byteLength)
        {
            var bytes = RandomNumberGenerator.GetBytes(byteLength);
            return Convert.ToBase64String(bytes).Replace('+', '-').Replace('/', '_').TrimEnd('=');
        }

        private string GetOrCreateAnonToken()
        {
            if (Request.Cookies.TryGetValue(AnonCookie, out var token) && !string.IsNullOrWhiteSpace(token))
                return token;

            var t = UrlSafeRandom(16); // 128-bit
            Response.Cookies.Append(AnonCookie, t, new CookieOptions
            {
                HttpOnly = true,
                SameSite = SameSiteMode.Lax,
                Secure = Request.IsHttps,
                Expires = DateTimeOffset.UtcNow.AddYears(1)
            });
            return t;
        }

        private static string EditCookieName(string slug) => $"sbedit_{slug}";

        private bool HasEditCookie(Storyboard sb)
        {
            if (string.IsNullOrWhiteSpace(sb.PublicId)) return false;
            if (!Request.Cookies.TryGetValue(EditCookieName(sb.PublicId), out var k) || string.IsNullOrWhiteSpace(k))
                return false;
            return string.Equals(Sha256Hex(k), sb.EditKeyHash, StringComparison.OrdinalIgnoreCase);
        }

        private bool IsOwnerByToken(Storyboard sb)
        {
            if (string.IsNullOrWhiteSpace(sb.OwnerTokenHash)) return false;
            if (!Request.Cookies.TryGetValue(AnonCookie, out var token) || string.IsNullOrWhiteSpace(token))
                return false;
            return string.Equals(Sha256Hex(token), sb.OwnerTokenHash, StringComparison.OrdinalIgnoreCase);
        }

        private bool IsOwnerByLogin(Storyboard sb)
            => !string.IsNullOrWhiteSpace(sb.OwnerId) && string.Equals(sb.OwnerId, CurrentUserId, StringComparison.Ordinal);

        private bool IsAuthenticated => User.Identity?.IsAuthenticated == true;

        // A shared (non-owner) visitor gets write access only when the owner
        // has opted in via both IsShared (link is public) AND AllowSharedEditing
        // (edits allowed). If the owner is authenticated, the visitor must also
        // be authenticated — anonymous collaboration is not allowed on logged-in
        // owners' boards.
        private bool HasSharedEditAccess(Storyboard sb)
        {
            if (!sb.IsShared || !sb.AllowSharedEditing) return false;
            if (!string.IsNullOrWhiteSpace(sb.OwnerId) && !IsAuthenticated) return false;
            return true;
        }

        private bool CanWrite(Storyboard sb)
            => IsOwnerByLogin(sb) || IsOwnerByToken(sb) || HasSharedEditAccess(sb);

        // Whether a (non-owner) visitor is allowed to even see the storyboard.
        // Owners always pass; everyone else needs IsShared = true and — when
        // the owner is authenticated — the visitor must be too.
        private bool CanAccessSharedBoard(Storyboard sb)
        {
            if (IsOwnerByLogin(sb) || IsOwnerByToken(sb)) return true;
            if (!sb.IsShared) return false;
            if (!string.IsNullOrWhiteSpace(sb.OwnerId) && !IsAuthenticated) return false;
            return true;
        }

        private static string NormalizePalette(string? raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return "";
            var parts = raw
                .Replace(";", ",").Replace("\n", ",").Replace("\r", ",")
                .Split(new[] { ',', ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(s => s.Trim())
                .Select(s => s.StartsWith('#') ? s : "#" + s)
                .Select(s => s.ToLowerInvariant())
                .Distinct();
            return string.Join(",", parts);
        }

        private async Task<string> NextPublicIdAsync()
        {
            string slug;
            do { slug = UrlSafeRandom(6); }
            while (await _context.Storyboards.AnyAsync(s => s.PublicId == slug));
            return slug;
        }

        private Task<Storyboard?> FindBySlugAsync(string slug, bool includeScenes = true, bool includeMarkers = false)
        {
            var q = _context.Storyboards.AsQueryable();
            if (includeScenes)
            {
                q = includeMarkers
                    ? q.Include(s => s.Scenes).ThenInclude(sc => sc.Markers)
                    : q.Include(s => s.Scenes);
            }
            return q.FirstOrDefaultAsync(s => s.PublicId == slug);
        }

        // ---------- routes ----------

        [HttpGet("")]
        public IActionResult Root() => RedirectToAction(nameof(My));

        [HttpGet("my")]
        public async Task<IActionResult> My()
        {
            if (User.Identity?.IsAuthenticated == true)
                return RedirectToAction("Index", "Storyboards", new { area = "User" });

            var ownerTokenHash = Sha256Hex(GetOrCreateAnonToken());

            var items = await _context.Storyboards
                .Where(s => s.OwnerTokenHash == ownerTokenHash)
                .OrderByDescending(s => s.CreatedAt)
                .ThenByDescending(s => s.Id)
                .Include(s => s.Scenes)
                .AsNoTracking()
                .ToListAsync();

            ViewBag.ImpersonatedEmail = null;
            ViewData["PublicIndex"] = true;
            return View("~/Areas/User/Views/Storyboards/Index.cshtml", items);
        }

        [HttpGet("create")]
        public IActionResult Create()
            => View("~/Areas/User/Views/Storyboards/Create.cshtml");

        [HttpPost("create")]
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
            [FromForm] string? licenseExtras)
        {
            if (string.IsNullOrWhiteSpace(title))
                ModelState.AddModelError("title", "Title is required");
            if (image == null || image.Length == 0)
                ModelState.AddModelError("image", "Cover-Bild ist erforderlich");
            if (image != null && image.Length > MaxImageSizeBytes)
                ModelState.AddModelError("image", "Bild darf maximal 2 MB groß sein.");
            if (!ModelState.IsValid)
                return View("~/Areas/User/Views/Storyboards/Create.cshtml");

            var webRoot = _env.WebRootPath ?? Path.Combine(_env.ContentRootPath, "wwwroot");
            var uploads = Path.Combine(webRoot, "uploads");
            Directory.CreateDirectory(uploads);

            var ext = Path.GetExtension(image.FileName).ToLowerInvariant();
            var allowed = new[] { ".png", ".jpg", ".jpeg", ".webp", ".gif" };
            if (!allowed.Contains(ext))
            {
                ModelState.AddModelError("image", "Only PNG/JPG/WebP/GIF are allowed");
                return View("~/Areas/User/Views/Storyboards/Create.cshtml");
            }

            var fileName = $"{Guid.NewGuid():N}{ext}";
            await using (var fs = System.IO.File.Create(Path.Combine(uploads, fileName)))
                await image.CopyToAsync(fs);

            var publicId = await NextPublicIdAsync();
            var editKeyPlain = UrlSafeRandom(24);
            var editKeyHash = Sha256Hex(editKeyPlain);
            var ownerTokenHash = Sha256Hex(GetOrCreateAnonToken());

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

                OwnerId = null,
                OwnerTokenHash = ownerTokenHash,

                PublicId = publicId,
                EditKeyHash = editKeyHash,
                Readonly = false,
                CreatedAt = DateTime.UtcNow,
                LastSeenAt = DateTime.UtcNow,
                Scenes = new List<Scene>(),
                CoverImagePath = "/uploads/" + fileName,
                ImagePath = "/uploads/" + fileName // legacy compatibility
            };

            _context.Storyboards.Add(sb);
            await _context.SaveChangesAsync();

            TempData["Info"] = $"Öffentlicher Link: /s/{publicId} • Edit-Key: {editKeyPlain}";
            Response.Cookies.Append(EditCookieName(publicId), editKeyPlain, new CookieOptions
            {
                HttpOnly = true,
                SameSite = SameSiteMode.Lax,
                Secure = Request.IsHttps,
                Expires = DateTimeOffset.UtcNow.AddDays(30)
            });

            return RedirectToAction(nameof(OpenBySlug), new { slug = publicId });
        }

        private static string? NormalizeAuthors(string? raw)
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

        // When the storyboard's owner is authenticated but the visitor is not,
        // we can't grant them access (per the shared-editing auth rule). Instead
        // of a silent forbid / read-only fallback, show a clear prompt so they
        // know to log in first.
        private bool RequiresLoginGate(Storyboard sb)
            => sb.IsShared
               && !string.IsNullOrWhiteSpace(sb.OwnerId)
               && !(User.Identity?.IsAuthenticated ?? false)
               && !IsOwnerByToken(sb);

        private IActionResult LoginRequiredView(string slug, string subPath)
        {
            var pathBase = Request.PathBase.HasValue ? Request.PathBase.Value : "";
            ViewBag.ReturnUrl = $"{pathBase}/s/{slug}{subPath}";
            return View("~/Areas/User/Views/Storyboards/LoginRequired.cshtml");
        }

        [HttpGet("{slug}")]
        public async Task<IActionResult> ViewBySlug(string slug, [FromQuery] string? k)
        {
            var sb = await FindBySlugAsync(slug, includeScenes: true, includeMarkers: true);
            if (sb == null) return NotFound();

            if (RequiresLoginGate(sb)) return LoginRequiredView(slug, "");
            if (!CanAccessSharedBoard(sb)) return NotFound();

            if (!string.IsNullOrWhiteSpace(k) &&
                string.Equals(Sha256Hex(k), sb.EditKeyHash, StringComparison.OrdinalIgnoreCase))
            {
                Response.Cookies.Append(EditCookieName(slug), k, new CookieOptions
                {
                    HttpOnly = true,
                    SameSite = SameSiteMode.Lax,
                    Secure = Request.IsHttps,
                    Expires = DateTimeOffset.UtcNow.AddDays(30)
                });
                TempData["Info"] = "Bearbeitung aktiviert (Edit-Key akzeptiert).";
            }

            int? activeSceneId = sb.Scenes?
                .OrderBy(sc => sc.Number).ThenBy(sc => sc.Id)
                .Select(sc => (int?)sc.Id)
                .FirstOrDefault();

            ViewBag.ActiveSceneId = activeSceneId;
            ViewBag.IsOwner = IsOwnerByLogin(sb) || IsOwnerByToken(sb);
            ViewData["PublicSlug"] = slug;
            return View("~/Areas/User/Views/Storyboards/Viewer.cshtml", sb);
        }

        [HttpGet("{slug}/open")]
        public async Task<IActionResult> OpenBySlug(string slug)
        {
            var sb = await FindBySlugAsync(slug, includeScenes: true, includeMarkers: true);
            if (sb == null) return NotFound();

            if (RequiresLoginGate(sb)) return LoginRequiredView(slug, "/open");
            if (!CanAccessSharedBoard(sb)) return NotFound();

            var isStaff = User.IsInRole("Admin") || User.IsInRole("SuperAdmin");
            var isOwner = IsOwnerByLogin(sb) || IsOwnerByToken(sb) || isStaff;

            // Non-owners only get the Builder when the owner has enabled
            // shared editing. Otherwise send them to the read-only Viewer.
            if (!isOwner && !sb.AllowSharedEditing)
                return RedirectToAction(nameof(ViewBySlug), new { slug });

            ViewBag.ActiveSceneId = sb.Scenes?
                .OrderBy(sc => sc.Number).ThenBy(sc => sc.Id)
                .Select(sc => (int?)sc.Id).FirstOrDefault();

            ViewData["PublicSlug"] = slug;

            var canWrite = CanWrite(sb);

            ViewData["ReadOnly"] = !canWrite;
            ViewBag.IsStaff = isStaff;
            ViewBag.CanGenerateAi = canWrite || isStaff;
            ViewBag.IsOwner = isOwner;
            ViewBag.InitialStep = 2;
            return View("~/Areas/User/Views/Storyboards/Builder.cshtml", sb);
        }

        [HttpGet("{slug}/edit")]
        public async Task<IActionResult> EditBySlug(string slug)
        {
            var sb = await FindBySlugAsync(slug, includeScenes: true, includeMarkers: true);
            if (sb == null) return NotFound();
            if (RequiresLoginGate(sb)) return LoginRequiredView(slug, "/edit");
            if (!CanAccessSharedBoard(sb)) return NotFound();
            // Non-owner without collab-edit rights → bounce to the read-only Viewer
            // rather than showing a raw 403.
            var isOwner = IsOwnerByLogin(sb) || IsOwnerByToken(sb)
                          || User.IsInRole("Admin") || User.IsInRole("SuperAdmin");
            if (!isOwner && !sb.AllowSharedEditing)
                return RedirectToAction(nameof(ViewBySlug), new { slug });
            if (!CanWrite(sb)) return Forbid();

            ViewData["PublicSlug"] = slug;
            ViewBag.InitialStep = 1;
            return View("~/Areas/User/Views/Storyboards/Builder.cshtml", sb);
        }

        [HttpPost("{slug}/edit")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditBySlugPost(
            string slug,
            [Bind("Id,Title,Zielgruppe,Beschreibung,Lernziel,Farbpalette,Taxonomie,License,Authors,LicenseExtras")] Storyboard input,
            IFormFile? image)
        {
            var sb = await FindBySlugAsync(slug, includeScenes: false);
            if (sb == null) return NotFound();
            if (!CanWrite(sb)) return Forbid();

            if (string.IsNullOrWhiteSpace(input.Title))
            {
                ModelState.AddModelError("Title", "Title is required");
                ViewData["PublicSlug"] = slug;
                return View("~/Areas/User/Views/Storyboards/Edit.cshtml", sb);
            }

            sb.Title = input.Title.Trim();
            sb.Zielgruppe = (input.Zielgruppe ?? "").Trim();
            sb.Beschreibung = (input.Beschreibung ?? "").Trim();
            sb.Lernziel = (input.Lernziel ?? "").Trim();
            sb.Farbpalette = NormalizePalette(input.Farbpalette);
            sb.Taxonomie = input.Taxonomie;
            sb.License = input.License;
            sb.Authors = NormalizeAuthors(input.Authors);
            sb.LicenseExtras = string.IsNullOrWhiteSpace(input.LicenseExtras) ? null : input.LicenseExtras.Trim();
            sb.LastSeenAt = DateTime.UtcNow;

            // handle COVER image upload (optional)
            if (image != null && image.Length > 0)
            {
                if (image.Length > MaxImageSizeBytes)
                {
                    ModelState.AddModelError("image", "Bild darf maximal 2 MB groß sein.");
                    ViewData["PublicSlug"] = slug;
                    return View("~/Areas/User/Views/Storyboards/Edit.cshtml", sb);
                }

                var webRoot = _env.WebRootPath ?? Path.Combine(_env.ContentRootPath, "wwwroot");
                var uploads = Path.Combine(webRoot, "uploads");
                Directory.CreateDirectory(uploads);

                var ext = Path.GetExtension(image.FileName).ToLowerInvariant();
                var allowed = new[] { ".png", ".jpg", ".jpeg", ".webp", ".gif" };
                if (!allowed.Contains(ext))
                {
                    ModelState.AddModelError("image", "Only PNG/JPG/WebP/GIF are allowed");
                    ViewData["PublicSlug"] = slug;
                    return View("~/Areas/User/Views/Storyboards/Edit.cshtml", sb);
                }

                var fileName = $"{Guid.NewGuid():N}{ext}";
                await using (var fs = System.IO.File.Create(Path.Combine(uploads, fileName)))
                    await image.CopyToAsync(fs);

                // delete old cover file
                var oldCover = !string.IsNullOrWhiteSpace(sb.CoverImagePath) ? sb.CoverImagePath : sb.ImagePath;
                if (!string.IsNullOrWhiteSpace(oldCover))
                {
                    var old = Path.Combine(webRoot, oldCover.TrimStart('/'));
                    if (System.IO.File.Exists(old)) System.IO.File.Delete(old);
                }

                var newPath = "/uploads/" + fileName;
                sb.CoverImagePath = newPath;
                sb.ImagePath = newPath; // legacy sync
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(OpenBySlug), new { slug });
        }

        [HttpPost("{slug}/edit-key")]
        public async Task<IActionResult> GenerateEditKey(string slug)
        {
            var sb = await _context.Storyboards.FirstOrDefaultAsync(s => s.PublicId == slug);
            if (sb == null) return NotFound();

            bool owns = (!string.IsNullOrEmpty(sb.OwnerId) && sb.OwnerId == CurrentUserId);
            if (!owns)
            {
                if (Request.Cookies.TryGetValue(AnonCookie, out var anonToken) && !string.IsNullOrWhiteSpace(anonToken))
                {
                    owns = string.Equals(Sha256Hex(anonToken), sb.OwnerTokenHash, StringComparison.OrdinalIgnoreCase);
                }
            }
            if (!owns) return Forbid();

            var plain = UrlSafeRandom(24);
            sb.EditKeyHash = Sha256Hex(plain);
            await _context.SaveChangesAsync();

            Response.Cookies.Append(EditCookieName(slug), plain, new CookieOptions
            {
                HttpOnly = true,
                SameSite = SameSiteMode.Lax,
                Secure = Request.IsHttps,
                Expires = DateTimeOffset.UtcNow.AddDays(30)
            });

            return Ok(new { key = plain });
        }

        [HttpPost("{slug}/delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteBySlug(string slug)
        {
            var sb = await _context.Storyboards
                .Include(s => s.Scenes)
                .FirstOrDefaultAsync(s => s.PublicId == slug);

            if (sb == null) return NotFound();
            if (!CanWrite(sb)) return Forbid();

            var webRoot = _env.WebRootPath ?? Path.Combine(_env.ContentRootPath, "wwwroot");
            foreach (var sc in sb.Scenes)
            {
                if (!string.IsNullOrWhiteSpace(sc.ImagePath))
                {
                    var path = Path.Combine(webRoot, sc.ImagePath.TrimStart('/'));
                    if (System.IO.File.Exists(path)) System.IO.File.Delete(path);
                }
            }
            var coverPath = !string.IsNullOrWhiteSpace(sb.CoverImagePath) ? sb.CoverImagePath : sb.ImagePath;
            if (!string.IsNullOrWhiteSpace(coverPath))
            {
                var cover = Path.Combine(webRoot, coverPath.TrimStart('/'));
                if (System.IO.File.Exists(cover)) System.IO.File.Delete(cover);
            }

            _context.Storyboards.Remove(sb);
            await _context.SaveChangesAsync();
            TempData["Info"] = "Storyboard gelöscht.";
            return RedirectToAction(nameof(My));
        }
    }
}
