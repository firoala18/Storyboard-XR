using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using System.IO;
using ProjectsWebApp.DataAccsess.Data;
using ProjectsWebApp.Models;
using ProjectsWebApp.Utility;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace ProjectsWebApp.Areas.User.Controllers
{
    [AllowAnonymous]
    [Area("User")]
    [Route("lp")]
    public class PublicLernpfadController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _env;

        private const long MaxImageSizeBytes = 2 * 1024 * 1024;

        public PublicLernpfadController(ApplicationDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        private const string AnonCookie = "sb_uid"; // reuse Storyboard anon identity
        private static string EditCookieName(string slug) => $"lpedit_{slug}";
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

            var t = UrlSafeRandom(16);
            Response.Cookies.Append(AnonCookie, t, new CookieOptions
            {
                HttpOnly = true,
                SameSite = SameSiteMode.Lax,
                Secure = Request.IsHttps,
                Expires = DateTimeOffset.UtcNow.AddYears(1)
            });
            return t;
        }

        // --- Image helpers ---
        private string EnsureUploadsDir()
        {
            var webRoot = _env.WebRootPath ?? Path.Combine(_env.ContentRootPath, "wwwroot");
            var uploads = Path.Combine(webRoot, "uploads");
            Directory.CreateDirectory(uploads);
            return uploads;
        }

        private string? SaveImage(IFormFile? file)
        {
            if (file == null || file.Length == 0) return null;
            if (file.Length > MaxImageSizeBytes) return null;
            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            var allowed = new[] { ".png", ".jpg", ".jpeg", ".webp", ".gif" };
            if (!allowed.Contains(ext)) return null;
            var uploads = EnsureUploadsDir();
            var name = $"{Guid.NewGuid():N}{ext}";
            var path = Path.Combine(uploads, name);
            using (var fs = new FileStream(path, FileMode.Create))
            {
                file.CopyTo(fs);
            }
            return "/uploads/" + name;
        }

        private Task<LernFlow?> FindBySlugAsync(string slug, bool includeSteps = true)
        {
            var q = _context.LernFlows.AsQueryable();
            if (includeSteps) q = q.Include(f => f.Steps);
            return q.FirstOrDefaultAsync(f => f.PublicId == slug);
        }

        private bool HasWriteAccess(LernFlow flow)
        {
            var uid = CurrentUserId;
            var isAdmin = User.IsInRole(SD.Role_Admin) || User.IsInRole("SuperAdmin");
            var anonHash = Sha256Hex(GetOrCreateAnonToken());
            var hasEditCookie = Request.Cookies.TryGetValue(EditCookieName(flow.PublicId), out var keyPlain)
                                 && !string.IsNullOrWhiteSpace(keyPlain)
                                 && Tokens.Sha256Hex(keyPlain) == flow.EditKeyHash;

            if (isAdmin) return true;
            if (!string.IsNullOrWhiteSpace(uid) && flow.OwnerId == uid) return true;
            if (!string.IsNullOrWhiteSpace(flow.OwnerTokenHash) && flow.OwnerTokenHash == anonHash) return true;
            if (hasEditCookie) return true;
            return false;
        }

        [HttpGet("")]
        public IActionResult Root() => RedirectToAction(nameof(My));

        [HttpGet("my")]
        public async Task<IActionResult> My([FromQuery] string? q)
        {
            var ownerTokenHash = Sha256Hex(GetOrCreateAnonToken());
            var flowsQ = _context.LernFlows
                .Where(f => f.OwnerTokenHash == ownerTokenHash)
                .Include(f => f.Steps)
                .AsNoTracking();

            if (!string.IsNullOrWhiteSpace(q))
            {
                var needle = q.Trim().ToLowerInvariant();
                flowsQ = flowsQ.Where(f => f.Title.ToLower().Contains(needle) || (f.Description ?? "").ToLower().Contains(needle));
                ViewBag.Query = q;
            }

            var flows = await flowsQ
                .OrderByDescending(f => f.CreatedAt).ThenByDescending(f => f.Id)
                .ToListAsync();

            ViewData["PublicIndex"] = true;
            return View("~/Areas/User/Views/Lernpfad/Index.cshtml", flows);
        }

        [HttpGet("create")]
        public IActionResult Create() => RedirectToAction(nameof(My));

        [HttpPost("create")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([FromForm] string title, [FromForm] string? description, IFormFile? flowImage)
        {
            var stepTitles = Request.Form["stepTitles[]"].ToArray();
            var stepDescs = Request.Form["stepDescriptions[]"].ToArray();

            if (string.IsNullOrWhiteSpace(title))
            {
                TempData["error"] = "Titel ist erforderlich.";
                return RedirectToAction(nameof(My));
            }

            // Generate unique slug
            string slug;
            do { slug = Tokens.NewSlug(8); } while (await _context.LernFlows.AnyAsync(f => f.PublicId == slug));
            var editKeyPlain = Tokens.NewEditKey();
            var editKeyHash = Tokens.Sha256Hex(editKeyPlain);

            // Owner: if user logged in, bind OwnerId; also set OwnerTokenHash for anon cookie identity
            var ownerId = CurrentUserId;
            var ownerTokenHash = Sha256Hex(GetOrCreateAnonToken());

            var flow = new LernFlow
            {
                Title = title.Trim(),
                Description = string.IsNullOrWhiteSpace(description) ? null : description!.Trim(),
                OwnerId = ownerId,
                OwnerTokenHash = ownerTokenHash,
                PublicId = slug,
                EditKeyHash = editKeyHash,
                CreatedAt = DateTime.UtcNow,
                LastSeenAt = DateTime.UtcNow,
                Steps = new List<LernStep>()
            };

            // collect steps
            var count = Math.Max(stepTitles.Length, stepDescs.Length);
            var keptIndex = 0; // index of non-empty steps for mapping images
            for (int i = 0; i < count; i++)
            {
                var st = (i < stepTitles.Length ? stepTitles[i] : string.Empty)?.Trim() ?? string.Empty;
                var sd = (i < stepDescs.Length ? stepDescs[i] : string.Empty)?.Trim();
                if (string.IsNullOrWhiteSpace(st)) continue; // skip empty titles
                var step = new LernStep { Title = st, Description = string.IsNullOrWhiteSpace(sd) ? null : sd, Order = flow.Steps.Count + 1 };
                // Try to map image by indexed key name set by JS on submit: stepImages[keptIndex]
                var fileKey = $"stepImages[{keptIndex}]";
                var f = Request.Form.Files.GetFile(fileKey);
                var imgPath = SaveImage(f);
                if (!string.IsNullOrWhiteSpace(imgPath)) step.ImagePath = imgPath;
                flow.Steps.Add(step);
                keptIndex++;
            }

            // optional cover image
            flow.ImagePath = SaveImage(flowImage);

            _context.LernFlows.Add(flow);
            await _context.SaveChangesAsync();

            // store edit cookie so user can edit later
            Response.Cookies.Append(EditCookieName(slug), editKeyPlain, new CookieOptions
            {
                HttpOnly = true,
                SameSite = SameSiteMode.Lax,
                Secure = Request.IsHttps,
                Expires = DateTimeOffset.UtcNow.AddDays(30)
            });

            TempData["success"] = "Lernpfad wurde erstellt.";
            return RedirectToAction(nameof(ViewBySlug), new { slug });
        }

        [HttpGet("{slug}")]
        public async Task<IActionResult> ViewBySlug(string slug)
        {
            var flow = await FindBySlugAsync(slug, includeSteps: true);
            if (flow == null) return NotFound();
            // Optional: Accept edit key via query parameter k
            if (Request.Query.TryGetValue("k", out var k) && !string.IsNullOrWhiteSpace(k))
            {
                var hash = Tokens.Sha256Hex(k!);
                if (hash == flow.EditKeyHash)
                {
                    Response.Cookies.Append(EditCookieName(slug), k!, new CookieOptions
                    {
                        HttpOnly = true,
                        SameSite = SameSiteMode.Lax,
                        Secure = Request.IsHttps,
                        Expires = DateTimeOffset.UtcNow.AddDays(30)
                    });
                    return RedirectToAction(nameof(ViewBySlug), new { slug });
                }
            }

            flow.Steps = flow.Steps.OrderBy(s => s.Order).ThenBy(s => s.Id).ToList();
            ViewData["PublicSlug"] = slug;
            ViewBag.CanEdit = HasWriteAccess(flow);
            return View("~/Areas/User/Views/Lernpfad/Details.cshtml", flow);
        }

        [HttpGet("{slug}/edit")]
        public async Task<IActionResult> Edit(string slug)
        {
            var flow = await FindBySlugAsync(slug, includeSteps: true);
            if (flow == null) return NotFound();
            if (!HasWriteAccess(flow)) return Forbid();

            flow.Steps = flow.Steps.OrderBy(s => s.Order).ThenBy(s => s.Id).ToList();
            ViewData["PublicSlug"] = slug;
            return View("~/Areas/User/Views/Lernpfad/Edit.cshtml", flow);
        }

        [HttpPost("{slug}/edit")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditSave(string slug, [FromForm] string title, [FromForm] string? description, IFormFile? flowImage)
        {
            var flow = await _context.LernFlows.Include(f => f.Steps).FirstOrDefaultAsync(f => f.PublicId == slug);
            if (flow == null) return NotFound();
            if (!HasWriteAccess(flow)) return Forbid();

            flow.Title = string.IsNullOrWhiteSpace(title) ? flow.Title : title.Trim();
            flow.Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
            flow.LastSeenAt = DateTime.UtcNow;

            var ids = Request.Form["stepIds[]"].Select(s => int.TryParse(s, out var id) ? id : 0).ToArray();
            var titles = Request.Form["stepTitles[]"].ToArray();
            var descs = Request.Form["stepDescriptions[]"].ToArray();
            var orders = Request.Form["stepOrders[]"].Select(s => int.TryParse(s, out var o) ? o : 0).ToArray();

            var seen = new HashSet<int>();
            var keptIndex = 0;
            for (int i = 0; i < Math.Max(Math.Max(ids.Length, titles.Length), Math.Max(descs.Length, orders.Length)); i++)
            {
                var id = i < ids.Length ? ids[i] : 0;
                var st = (i < titles.Length ? titles[i] : string.Empty)?.Trim() ?? string.Empty;
                var sd = (i < descs.Length ? descs[i] : string.Empty)?.Trim();
                var ord = i < orders.Length && orders[i] > 0 ? orders[i] : (i + 1);
                if (string.IsNullOrWhiteSpace(st)) continue;

                if (id > 0)
                {
                    var ex = flow.Steps.FirstOrDefault(s => s.Id == id);
                    if (ex != null)
                    {
                        ex.Title = st;
                        ex.Description = string.IsNullOrWhiteSpace(sd) ? null : sd;
                        ex.Order = ord;
                        // map new image if provided
                        var fileKey = $"stepImages[{keptIndex}]";
                        var f = Request.Form.Files.GetFile(fileKey);
                        var imgPath = SaveImage(f);
                        if (!string.IsNullOrWhiteSpace(imgPath))
                        {
                            ex.ImagePath = imgPath;
                        }
                        seen.Add(id);
                    }
                }
                else
                {
                    var step = new LernStep { Title = st, Description = string.IsNullOrWhiteSpace(sd) ? null : sd, Order = ord };
                    var fileKey = $"stepImages[{keptIndex}]";
                    var f = Request.Form.Files.GetFile(fileKey);
                    var imgPath = SaveImage(f);
                    if (!string.IsNullOrWhiteSpace(imgPath)) step.ImagePath = imgPath;
                    flow.Steps.Add(step);
                }

                keptIndex++;
            }

            // delete removed steps
            var toRemove = flow.Steps.Where(s => !seen.Contains(s.Id) && ids.Contains(s.Id) == false).ToList();
            foreach (var r in toRemove) _context.Remove(r);

            await _context.SaveChangesAsync();
            TempData["success"] = "Lernpfad gespeichert.";
            return RedirectToAction(nameof(ViewBySlug), new { slug });
        }

        [HttpPost("{slug}/delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(string slug)
        {
            var flow = await _context.LernFlows.Include(f => f.Steps).FirstOrDefaultAsync(f => f.PublicId == slug);
            if (flow == null) return NotFound();
            if (!HasWriteAccess(flow)) return Forbid();

            _context.LernFlows.Remove(flow);
            await _context.SaveChangesAsync();
            TempData["success"] = "Lernpfad gelöscht.";
            return RedirectToAction(nameof(My));
        }
    }
}
