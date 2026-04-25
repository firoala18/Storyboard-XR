using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using ProjectsWebApp.DataAccsess.Data;
using ProjectsWebApp.Hubs;
using ProjectsWebApp.Models;
using ProjectsWebApp.Models.Dtos;
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
        private readonly IHubContext<StoryboardHub> _hub;

        public StoryboardsController(
            ApplicationDbContext context,
            IWebHostEnvironment env,
            UserManager<IdentityUser> userManager,
            IHubContext<StoryboardHub> hub)
        {
            _context = context;
            _env = env;
            _userManager = userManager;
            _hub = hub;
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

        // GET: /User/Storyboards/Details/5  — redirect shim to the Builder.
        [AllowAnonymous]
        public IActionResult Details(int id, int? sceneId, string? t)
        {
            var url = Url.Action(nameof(Builder), new { id, step = 2, t });
            return RedirectPermanent(url ?? $"/Storyboards/Builder/{id}?step=2");
        }

        // POST: /User/Storyboards/NewDraft
        // Creates a minimal placeholder storyboard for the inline Builder flow.
        // The Builder page then auto-saves title, description, cover, etc. via PATCH.
        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> NewDraft()
        {
            var userId = CurrentUserId;
            var ownerTokenHash = string.IsNullOrWhiteSpace(userId)
                ? GetOrCreateOwnerTokenHash(HttpContext)
                : null;

            var publicId = await NextPublicIdAsync();
            var editKeyPlain = UrlSafeRandom(24);
            var editKeyHash = Sha256Hex(editKeyPlain);

            var sb = new Storyboard
            {
                Title = "Unbenanntes Storyboard",
                OwnerId = userId,
                OwnerTokenHash = ownerTokenHash,
                PublicId = publicId,
                EditKeyHash = editKeyHash,
                CreatedAt = DateTime.UtcNow,
                LastSeenAt = DateTime.UtcNow,
                CoverImagePath = DefaultCoverImagePath,
                ImagePath = DefaultCoverImagePath,
            };

            _context.Storyboards.Add(sb);
            await _context.SaveChangesAsync();

            return Json(new { id = sb.Id });
        }

        // GET: /User/Storyboards/Builder/5[?step=1|2|3][&t=EDIT_TOKEN]
        // The unified edit surface: timeline header + three step panels (Infos / Scenes / PDF).
        // Allow owner/super, or anonymous with a valid edit token / edit cookie.
        [AllowAnonymous]
        public async Task<IActionResult> Builder(int id, int step = 1, string? t = null)
        {
            var sb = await _context.Storyboards
                .Include(s => s.Scenes.OrderBy(x => x.Number))
                    .ThenInclude(s => s.Markers.OrderBy(m => m.Number))
                .FirstOrDefaultAsync(s => s.Id == id);
            if (sb == null) return NotFound();

            var hasEditToken = !string.IsNullOrWhiteSpace(t) && t == sb.AccessTokenEdit;
            if (!hasEditToken && !CanWrite(sb)) return Forbid();

            sb.LastSeenAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            ViewBag.Token = t;
            ViewBag.InitialStep = step is >= 1 and <= 3 ? step : 1;
            ViewBag.IsOwner = IsOwner(sb) || IsStaff;
            return View("Builder", sb);
        }

        // GET /Storyboards/{id}/ScenesJson
        // Fresh snapshot of a storyboard's scenes + markers. Used by Step 3 to
        // rebuild its preview after edits made in Step 2 without a page reload.
        [AllowAnonymous]
        [HttpGet("/Storyboards/{id:int}/ScenesJson")]
        public async Task<IActionResult> ScenesJson(int id)
        {
            var sb = await _context.Storyboards
                .Include(s => s.Scenes)
                    .ThenInclude(sc => sc.Markers)
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.Id == id);
            if (sb == null) return NotFound();
            if (!CanWrite(sb)) return Forbid();

            return Json(new
            {
                id = sb.Id,
                title = sb.Title,
                scenes = sb.Scenes.OrderBy(s => s.Number).ThenBy(s => s.Id).Select(s => new
                {
                    id = s.Id,
                    number = s.Number,
                    name = s.Name,
                    imagePath = string.IsNullOrWhiteSpace(s.ImagePath) ? null : Url.Content("~" + s.ImagePath),
                    markers = s.Markers.OrderBy(m => m.Number).Select(m => new
                    {
                        id = m.Id,
                        number = m.Number,
                        x = m.X,
                        y = m.Y,
                        colorHex = m.ColorHex,
                        description = m.Description
                    })
                })
            });
        }

        private const long MaxCoverImageBytes = 30L * 1024 * 1024; // 30 MB
        private static readonly string[] AllowedCoverExtensions = { ".png", ".jpg", ".jpeg", ".webp", ".gif" };
        private const string DefaultCoverImagePath = "/images/no-image-icon.png";

        // POST: /User/Storyboards/UploadCover
        // Stores a new cover image and updates Storyboard.CoverImagePath.
        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequestSizeLimit(40_000_000)]
        [RequestFormLimits(MultipartBodyLengthLimit = 40_000_000)]
        public async Task<IActionResult> UploadCover(int id, IFormFile image)
        {
            var sb = await _context.Storyboards.FirstOrDefaultAsync(s => s.Id == id);
            if (sb == null) return NotFound();
            if (!CanWrite(sb)) return Forbid();
            if (image == null || image.Length == 0) return BadRequest(new { message = "Keine Datei empfangen." });
            if (image.Length > MaxCoverImageBytes) return BadRequest(new { message = "Max 30 MB." });

            var relPath = await SaveCoverImageAsync(image, sb);
            if (relPath == null) return BadRequest(new { message = "Nur PNG/JPG/WebP/GIF erlaubt." });

            sb.CoverImagePath = relPath;
            sb.ImagePath = relPath; // legacy sync
            sb.LastSeenAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return Json(new { coverImagePath = Url.Content("~" + relPath) });
        }

        private async Task<string?> SaveCoverImageAsync(IFormFile image, Storyboard sb)
        {
            var ext = Path.GetExtension(image.FileName).ToLowerInvariant();
            if (!AllowedCoverExtensions.Contains(ext)) return null;

            var webRoot = _env.WebRootPath ?? Path.Combine(_env.ContentRootPath, "wwwroot");
            var uploadsDir = Path.Combine(webRoot, "uploads");
            Directory.CreateDirectory(uploadsDir);

            var fileName = $"{Guid.NewGuid():N}{ext}";
            var dest = Path.Combine(uploadsDir, fileName);
            await using (var fs = new FileStream(dest, FileMode.Create))
                await image.CopyToAsync(fs);

            var existing = !string.IsNullOrWhiteSpace(sb.CoverImagePath) ? sb.CoverImagePath : sb.ImagePath;
            if (!string.IsNullOrWhiteSpace(existing) && existing.StartsWith("/uploads/", StringComparison.OrdinalIgnoreCase))
            {
                var oldPath = Path.Combine(webRoot, existing.TrimStart('/'));
                if (System.IO.File.Exists(oldPath))
                {
                    try { System.IO.File.Delete(oldPath); } catch { /* best-effort */ }
                }
            }

            return "/uploads/" + fileName;
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

        // ---- Public editing auth helpers (mirror ScenesController lines 43–62) ----
        private const string AnonCookie = "sb_uid";
        private static string EditCookieName(string slug) => $"sbedit_{slug}";

        private bool IsStaff => User.IsInRole("Admin") || User.IsInRole("SuperAdmin");

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

        private bool HasSharedEditAccess(Storyboard sb)
        {
            if (!sb.IsShared || !sb.AllowSharedEditing) return false;
            if (!string.IsNullOrWhiteSpace(sb.OwnerId) && !IsAuthenticated) return false;
            return true;
        }

        private bool IsOwner(Storyboard sb) => IsOwnerByLogin(sb) || IsOwnerByToken(sb);

        private bool CanWrite(Storyboard sb)
            => IsStaff || IsOwner(sb) || HasSharedEditAccess(sb);

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

        // GET: /User/Storyboards/Edit/5  — redirect shim to the Builder.
        [AllowAnonymous]
        public IActionResult Edit(int id, string? t)
        {
            var url = Url.Action(nameof(Builder), new { id, step = 1, t });
            return RedirectPermanent(url ?? $"/Storyboards/Builder/{id}?step=1");
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

            // remove cover file (new field preferred) and legacy — only user uploads, not shared defaults
            var coverPath = !string.IsNullOrWhiteSpace(sb.CoverImagePath) ? sb.CoverImagePath : sb.ImagePath;
            if (!string.IsNullOrWhiteSpace(coverPath) && coverPath.StartsWith("/uploads/", StringComparison.OrdinalIgnoreCase))
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
                    .ThenInclude(sc => sc.Markers)
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
            ViewBag.IsOwner = isOwner || IsSuper;
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

        // PATCH /Storyboards/{id}
        // Partial update for inline auto-save. Each property is optional;
        // null means "not sent; don't change", non-null means "update to this value".
        [AllowAnonymous]
        [HttpPatch("/Storyboards/{id:int}")]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> Patch(int id, [FromBody] StoryboardPatchDto dto)
        {
            if (dto == null) return BadRequest();

            var sb = await _context.Storyboards.FirstOrDefaultAsync(s => s.Id == id);
            if (sb == null) return NotFound();
            if (!CanWrite(sb)) return Forbid();

            if (dto.RowVersion != null && sb.RowVersion != null &&
                !dto.RowVersion.SequenceEqual(sb.RowVersion))
            {
                return Conflict(new { reason = "stale", rowVersion = sb.RowVersion });
            }

            if (dto.Title is not null)
            {
                var t = dto.Title.Trim();
                if (string.IsNullOrWhiteSpace(t)) return BadRequest(new { field = "title", message = "Titel darf nicht leer sein." });
                if (t.Length > 200) return BadRequest(new { field = "title", message = "Max. 200 Zeichen." });
                sb.Title = t;
            }
            if (dto.Zielgruppe is not null) sb.Zielgruppe = dto.Zielgruppe;
            if (dto.Beschreibung is not null) sb.Beschreibung = dto.Beschreibung;
            if (dto.Lernziel is not null) sb.Lernziel = dto.Lernziel;
            if (dto.Farbpalette is not null) sb.Farbpalette = dto.Farbpalette;
            if (dto.Taxonomie is not null) sb.Taxonomie = dto.Taxonomie.Value;
            if (dto.License is not null) sb.License = dto.License.Value;
            if (dto.LicenseExtras is not null)
            {
                if (dto.LicenseExtras.Length > 2000) return BadRequest(new { field = "licenseExtras", message = "Max. 2000 Zeichen." });
                sb.LicenseExtras = dto.LicenseExtras;
            }
            if (dto.Authors is not null) sb.Authors = dto.Authors;
            if (dto.CoverImagePath is not null) sb.CoverImagePath = dto.CoverImagePath;

            // Toggling sharing/edit permission is owner-only. Collaborators
            // holding write access (via the toggle itself) must not be able to
            // revoke or alter it, so we enforce IsOwner explicitly here.
            // Disabling IsShared cascades to AllowSharedEditing — you can't have
            // shared editing on a board that isn't even shared.
            if (dto.IsShared is not null)
            {
                if (!IsStaff && !IsOwner(sb)) return Forbid();
                sb.IsShared = dto.IsShared.Value;
                if (!sb.IsShared) sb.AllowSharedEditing = false;
            }
            if (dto.AllowSharedEditing is not null)
            {
                if (!IsStaff && !IsOwner(sb)) return Forbid();
                // Enabling collab-edit requires the board to be shared first.
                sb.AllowSharedEditing = dto.AllowSharedEditing.Value && sb.IsShared;
            }

            sb.LastSeenAt = DateTime.UtcNow;

            try { await _context.SaveChangesAsync(); }
            catch (DbUpdateConcurrencyException) { return Conflict(new { reason = "stale" }); }

            var changes = new Dictionary<string, object?>();
            if (dto.Title is not null) changes["title"] = sb.Title;
            if (dto.Zielgruppe is not null) changes["zielgruppe"] = sb.Zielgruppe;
            if (dto.Beschreibung is not null) changes["beschreibung"] = sb.Beschreibung;
            if (dto.Lernziel is not null) changes["lernziel"] = sb.Lernziel;
            if (dto.Farbpalette is not null) changes["farbpalette"] = sb.Farbpalette;
            if (dto.Taxonomie is not null) changes["taxonomie"] = (int)sb.Taxonomie!;
            if (dto.License is not null) changes["license"] = (int?)sb.License;
            if (dto.LicenseExtras is not null) changes["licenseExtras"] = sb.LicenseExtras;
            if (dto.Authors is not null) changes["authors"] = sb.Authors;
            if (dto.CoverImagePath is not null) changes["coverImagePath"] = sb.CoverImagePath;
            if (dto.IsShared is not null) changes["isShared"] = sb.IsShared;
            if (dto.AllowSharedEditing is not null) changes["allowSharedEditing"] = sb.AllowSharedEditing;

            if (changes.Count > 0 && sb.AllowSharedEditing)
            {
                await _hub.Clients.Group($"sb-{id}").SendAsync("StoryboardUpdated", new
                {
                    id,
                    origin = HttpContext.Connection.Id,
                    fields = changes
                });
            }

            return Ok(new { ok = true, lastSavedAt = DateTime.UtcNow, rowVersion = sb.RowVersion });
        }
    }
}
