using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using ProjectsWebApp.DataAccsess.Data;
using ProjectsWebApp.Hubs;
using ProjectsWebApp.Models;

namespace ProjectsWebApp.Areas.User.Controllers
{
    [Area("User")]
    [AllowAnonymous] // allow anonymous access by default; secure specific actions below
    public class ScenesController(ApplicationDbContext context, IWebHostEnvironment env, IHubContext<StoryboardHub> hub, IConfiguration config) : Controller
    {
        private readonly ApplicationDbContext _context = context;
        private readonly IWebHostEnvironment _env = env;
        private readonly IHubContext<StoryboardHub> _hub = hub;
        private readonly IConfiguration _config = config;

        private const long MaxImageSizeBytes = 2 * 1024 * 1024;

        // Only used on actions that require [Authorize]
        private string? CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier);
        private bool IsStaff => User.IsInRole("Admin") || User.IsInRole("SuperAdmin");

        // ---- Public editing auth helpers (mirror PublicStoryboardsController) ----
        private const string AnonCookie = "sb_uid";
        private static string EditCookieName(string slug) => $"sbedit_{slug}";

        private static string Sha256Hex(string value)
        {
            var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(value));
            return Convert.ToHexString(bytes).ToLowerInvariant();
        }

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

        private bool CanWrite(Storyboard sb) => IsStaff || IsOwnerByLogin(sb) || IsOwnerByToken(sb) || HasEditCookie(sb);

        // POST: /User/Scenes/GenerateAiScene
        // Generate a new scene image via OpenAI (gpt-image-1) and create a Scene entry.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GenerateAiScene(int storyboardId, string prompt, string? aspect, string? quality)
        {
            var sb = await _context.Storyboards
                .Include(s => s.Scenes)
                .FirstOrDefaultAsync(s => s.Id == storyboardId);

            if (sb == null)
                return NotFound();
            if (!CanWrite(sb))
                return Forbid();

            if (string.IsNullOrWhiteSpace(prompt))
                return BadRequest(new { error = "Prompt darf nicht leer sein." });

            // Map gewünschtes Seitenverhältnis auf die von gpt-image-1 unterstützten Größen
            // Verfügbar laut Doku: 1024x1024 (1:1), 1536x1024 (Landscape), 1024x1536 (Portrait)
            var size = aspect switch
            {
                "16:9" => "1536x1024",  // breites Querformat
                "4:3"  => "1536x1024",  // etwas weniger breit, aber gleiche Basisgröße
                "9:16" => "1024x1536",  // hohes Hochformat
                "3:4"  => "1024x1536",  // ähnlich
                _       => "1024x1024"   // 1:1 Standard
            };

            // Qualität aus UI (low/medium/high/auto), Default = low (günstig)
            var q = (quality ?? "low").Trim().ToLowerInvariant();
            var qual = q switch
            {
                "medium" => "medium",
                "high"   => "high",
                "auto"   => "auto",
                _         => "low"
            };

            // Bestimme nächste freie Szenennummer und lege Szene VOR der Generierung an
            var usedNumbers = (sb.Scenes ?? new List<Scene>()).Select(s => s.Number).ToList();
            var desired = usedNumbers.Count == 0 ? 1 : usedNumbers.Max() + 1;
            if (desired <= 0) desired = 1;
            while (usedNumbers.Contains(desired)) desired++;

            // Platzhalter-Bild (Loading-GIF) während der Generierung
            var placeholderPath = "/images/image_generating_loading.gif";

            var sc = new Scene
            {
                StoryboardId = storyboardId,
                Number = desired,
                Name = "KI-Szene",
                ImagePath = placeholderPath
            };

            _context.Scenes.Add(sc);
            await _context.SaveChangesAsync();

            // Realtime: neue Szene mit Platzhalter-Bild
            await _hub.Clients.Group($"sb-{storyboardId}").SendAsync("SceneCreated", new
            {
                sc.Id,
                sc.Number,
                sc.Name,
                sc.ImagePath,
                sc.StoryboardId
            });

            // Prefer configuration value, fall back to environment variable
            var apiKey = _config["OpenAI:ApiKey"];
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
            }
            if (string.IsNullOrWhiteSpace(apiKey))
                return StatusCode(500, new { error = "OpenAI API Key ist nicht konfiguriert." });

            try
            {
                using var http = new HttpClient
                {
                    Timeout = TimeSpan.FromMinutes(2)
                };
                http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

                var payload = new
                {
                    model = "gpt-image-1",
                    prompt,
                    size,
                    quality = qual
                };

                var json = JsonSerializer.Serialize(payload);
                using var content = new StringContent(json, Encoding.UTF8, "application/json");
                using var res = await http.PostAsync("https://api.openai.com/v1/images/generations", content);

                var resText = await res.Content.ReadAsStringAsync();
                if (!res.IsSuccessStatusCode)
                {
                    return StatusCode((int)res.StatusCode, new { error = "Bildgenerierung fehlgeschlagen.", detail = resText });
                }

                using var doc = JsonDocument.Parse(resText);
                var root = doc.RootElement;
                if (!root.TryGetProperty("data", out var dataArr) || dataArr.GetArrayLength() == 0)
                    return StatusCode(500, new { error = "Die API hat kein Bild zurückgegeben." });

                var b64 = dataArr[0].GetProperty("b64_json").GetString();
                if (string.IsNullOrWhiteSpace(b64))
                    return StatusCode(500, new { error = "Die API hat kein Bild zurückgegeben." });

                byte[] bytes;
                try
                {
                    bytes = Convert.FromBase64String(b64);
                }
                catch
                {
                    return StatusCode(500, new { error = "Antwort konnte nicht dekodiert werden." });
                }

                var webRoot = _env.WebRootPath;
                if (string.IsNullOrEmpty(webRoot))
                    webRoot = Path.Combine(_env.ContentRootPath, "wwwroot");

                var uploads = Path.Combine(webRoot, "uploads");
                Directory.CreateDirectory(uploads);
                var fileName = $"{Guid.NewGuid():N}.png";
                var filePath = Path.Combine(uploads, fileName);
                await System.IO.File.WriteAllBytesAsync(filePath, bytes);

                // Szene auf das finale Bild aktualisieren
                sc.ImagePath = "/uploads/" + fileName;
                await _context.SaveChangesAsync();

                string? redirectUrl;
                if (!string.IsNullOrWhiteSpace(sb.PublicId))
                {
                    redirectUrl = Url.Action("OpenBySlug", "PublicStoryboards", new { area = "User", slug = sb.PublicId, sceneId = sc.Id });
                }
                else
                {
                    redirectUrl = Url.Action("Details", "Storyboards", new { area = "User", id = storyboardId, sceneId = sc.Id });
                }

                return Ok(new { success = true, url = redirectUrl });
            }
            catch
            {
                // Bei Fehler die zuvor angelegte Szene wieder entfernen, damit keine "hängenden" Platzhalter bleiben
                _context.Scenes.Remove(sc);
                await _context.SaveChangesAsync();
                await _hub.Clients.Group($"sb-{storyboardId}").SendAsync("SceneDeleted", new { id = sc.Id, storyboardId });
                return StatusCode(500, new { error = "Interner Fehler bei der Bildgenerierung." });
            }
        }

        // GET: /User/Scenes/Create?storyboardId=5
        public async Task<IActionResult> Create(int storyboardId)
        {
            var sb = await _context.Storyboards.FindAsync(storyboardId);
            if (sb == null)
                return NotFound();
            if (!CanWrite(sb))
                return Forbid();

            // Suggest next free number
            var next = (await _context.Scenes
                            .Where(s => s.StoryboardId == storyboardId)
                            .Select(s => (int?)s.Number)
                            .MaxAsync()) ?? 0;

            ViewBag.Storyboard = sb;
            ViewBag.SuggestedNumber = next + 1;
            return View();
        }

        // POST: /User/Scenes/Create
        // Increase limits in case large images are uploaded (default server/body limits can yield 400 Bad Request)
        [HttpPost, ValidateAntiForgeryToken]
        [RequestSizeLimit(64_000_000)] // 64 MB
        [RequestFormLimits(MultipartBodyLengthLimit = 64_000_000)]
        public async Task<IActionResult> Create(int storyboardId, int number, IFormFile image, string? name)
        {
            // If a previous oversize attempt happened, ModelState will be invalid with a form error
            if (!ModelState.IsValid && ModelState.TryGetValue(string.Empty, out var entry) && entry.Errors.Count > 0)
            {
                // Show a friendlier message
                ModelState.AddModelError("image", "Die Datei ist zu groß. Bitte wähle ein Bild bis 64 MB.");
            }

            var sb = await _context.Storyboards.FindAsync(storyboardId);
            if (sb == null)
                return NotFound();
            if (!CanWrite(sb))
                return Forbid();

            if (image == null || image.Length == 0)
            {
                ModelState.AddModelError("image", "Bild erforderlich");
                ViewBag.Storyboard = sb;
                ViewBag.SuggestedNumber = number <= 0 ? 1 : number;
                return View();
            }

            if (image != null && image.Length > MaxImageSizeBytes)
            {
                ModelState.AddModelError("image", "Bild darf maximal 2 MB groß sein.");
                ViewBag.Storyboard = sb;
                ViewBag.SuggestedNumber = number <= 0 ? 1 : number;
                return View();
            }

            var allowed = new[] { ".png", ".jpg", ".jpeg", ".webp", ".gif" };
            var ext = Path.GetExtension(image.FileName).ToLowerInvariant();
            if (!allowed.Contains(ext))
            {
                ModelState.AddModelError("image", "Nur PNG/JPG/WEBP/GIF");
                ViewBag.Storyboard = sb;
                ViewBag.SuggestedNumber = number <= 0 ? 1 : number;
                return View();
            }

            // Unique number within storyboard (auto-bump if taken)
            var used = await _context.Scenes
                                     .Where(s => s.StoryboardId == storyboardId)
                                     .Select(s => s.Number)
                                     .ToListAsync();

            var desired = Math.Max(1, number <= 0 ? 1 : number);
            while (used.Contains(desired)) desired++;

            if (desired != number)
                TempData["Info"] = $"Nummer {number} war bereits vergeben. Szene wurde als Nr. {desired} angelegt.";

            // Save image (robust web root fallback)
            var webRoot = _env.WebRootPath;
            if (string.IsNullOrEmpty(webRoot))
                webRoot = Path.Combine(_env.ContentRootPath, "wwwroot");

            var uploads = Path.Combine(webRoot, "uploads");
            Directory.CreateDirectory(uploads);
            var fileName = $"{Guid.NewGuid():N}{ext}";
            try
            {
                await using (var fs = System.IO.File.Create(Path.Combine(uploads, fileName)))
                    await image.CopyToAsync(fs);
            }
            catch (InvalidDataException)
            {
                // Multipart length exceeded
                ModelState.AddModelError("image", "Die Datei ist zu groß. Bitte wähle ein Bild bis 64 MB.");
                ViewBag.Storyboard = sb;
                ViewBag.SuggestedNumber = number <= 0 ? 1 : number;
                return View();
            }

            var sc = new Scene
            {
                StoryboardId = storyboardId,
                Number = desired,
                Name = string.IsNullOrWhiteSpace(name) ? null : name.Trim(),
                ImagePath = "/uploads/" + fileName
            };

            _context.Scenes.Add(sc);
            await _context.SaveChangesAsync();

            // Realtime broadcast: new scene
            await _hub.Clients.Group($"sb-{storyboardId}").SendAsync("SceneCreated", new
            {
                sc.Id,
                sc.Number,
                sc.Name,
                sc.ImagePath,
                sc.StoryboardId
            });

            if (!string.IsNullOrWhiteSpace(sb.PublicId))
                return RedirectToAction("OpenBySlug", "PublicStoryboards", new { area = "User", slug = sb.PublicId });
            return RedirectToAction("Details", "Storyboards", new { area = "User", id = storyboardId, sceneId = sc.Id });
        }

        // GET: /User/Scenes/Edit/12
        public async Task<IActionResult> Edit(int id)
        {
            var sc = await _context.Scenes
                                   .Include(s => s.Storyboard)
                                   .FirstOrDefaultAsync(s => s.Id == id);
            if (sc == null || sc.Storyboard == null)
                return NotFound();
            if (!CanWrite(sc.Storyboard))
                return Forbid();

            return View(sc);
        }

        // POST: /User/Scenes/Edit/12
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, int number, string? name, IFormFile? image)
        {
            var sc = await _context.Scenes
                                   .Include(s => s.Storyboard)
                                   .FirstOrDefaultAsync(s => s.Id == id);
            if (sc == null || sc.Storyboard == null)
                return NotFound();
            if (!CanWrite(sc.Storyboard))
                return Forbid();

            // Keep number unique within the storyboard (auto-bump)
            var desired = Math.Max(1, number <= 0 ? 1 : number);
            var used = await _context.Scenes
                                     .Where(s => s.StoryboardId == sc.StoryboardId && s.Id != sc.Id)
                                     .Select(s => s.Number)
                                     .ToListAsync();
            var original = desired;
            while (used.Contains(desired)) desired++;

            if (desired != original)
                TempData["Info"] = $"Nummer {original} war bereits vergeben. Szene wurde als Nr. {desired} gespeichert.";

            sc.Number = desired;
            sc.Name = string.IsNullOrWhiteSpace(name) ? null : name.Trim();

            if (image != null && image.Length > 0)
            {
                if (image.Length > MaxImageSizeBytes)
                {
                    ModelState.AddModelError("image", "Bild darf maximal 2 MB groß sein.");
                    return View(sc);
                }

                var allowed = new[] { ".png", ".jpg", ".jpeg", ".webp", ".gif" };
                var ext = Path.GetExtension(image.FileName).ToLowerInvariant();
                if (!allowed.Contains(ext))
                {
                    ModelState.AddModelError("image", "Nur PNG/JPG/WEBP/GIF");
                    return View(sc);
                }

                // robust web root
                var webRoot = _env.WebRootPath;
                if (string.IsNullOrEmpty(webRoot))
                    webRoot = Path.Combine(_env.ContentRootPath, "wwwroot");

                var uploads = Path.Combine(webRoot, "uploads");
                Directory.CreateDirectory(uploads);
                var fileName = $"{Guid.NewGuid():N}{ext}";
                await using (var fs = System.IO.File.Create(Path.Combine(uploads, fileName)))
                    await image.CopyToAsync(fs);

                // delete old file
                if (!string.IsNullOrWhiteSpace(sc.ImagePath))
                {
                    var old = Path.Combine(webRoot, sc.ImagePath.TrimStart('/'));
                    if (System.IO.File.Exists(old)) System.IO.File.Delete(old);
                }

                sc.ImagePath = "/uploads/" + fileName;
            }

            await _context.SaveChangesAsync();
            if (!string.IsNullOrWhiteSpace(sc.Storyboard.PublicId))
                return RedirectToAction("OpenBySlug", "PublicStoryboards", new { area = "User", slug = sc.Storyboard.PublicId });
            return RedirectToAction("Details", "Storyboards", new { area = "User", id = sc.StoryboardId, sceneId = sc.Id });
        }

        // POST: /User/Scenes/Delete
        // NOTE: Single Delete endpoint to avoid AmbiguousMatchException
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id, int storyboardId)
        {
            var scene = await _context.Scenes
                .Include(s => s.Storyboard)
                .Include(s => s.Markers) // ok if you have markers; cascade works too
                .FirstOrDefaultAsync(s => s.Id == id && s.StoryboardId == storyboardId);

            if (scene == null || scene.Storyboard == null)
                return NotFound();
            if (!CanWrite(scene.Storyboard))
                return Forbid();

            // remove image file
            var webRoot = _env.WebRootPath;
            if (string.IsNullOrEmpty(webRoot))
                webRoot = Path.Combine(_env.ContentRootPath, "wwwroot");

            if (!string.IsNullOrWhiteSpace(scene.ImagePath))
            {
                var path = Path.Combine(webRoot, scene.ImagePath.TrimStart('/'));
                if (System.IO.File.Exists(path)) System.IO.File.Delete(path);
            }

            _context.Scenes.Remove(scene);
            await _context.SaveChangesAsync();

            // Realtime broadcast: scene deleted
            await _hub.Clients.Group($"sb-{storyboardId}").SendAsync("SceneDeleted", new { id, storyboardId });

            // pick next scene to open (if any)
            var nextSceneId = await _context.Scenes
                .Where(s => s.StoryboardId == storyboardId)
                .OrderBy(s => s.Number).ThenBy(s => s.Id)
                .Select(s => (int?)s.Id)
                .FirstOrDefaultAsync();

            TempData["Info"] = "Szene wurde gelöscht.";
            if (!string.IsNullOrWhiteSpace(scene.Storyboard.PublicId))
                return RedirectToAction("OpenBySlug", "PublicStoryboards", new { area = "User", slug = scene.Storyboard.PublicId });
            return RedirectToAction("Details", "Storyboards", new { area = "User", id = storyboardId, sceneId = nextSceneId });
        }
    }
}
