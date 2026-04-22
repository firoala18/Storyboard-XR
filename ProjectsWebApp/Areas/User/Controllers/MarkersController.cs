using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.SignalR;
using ProjectsWebApp.DataAccsess.Data;
using ProjectsWebApp.Models;
using ProjectsWebApp.Models.Dtos;
using ProjectsWebApp.Hubs;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace ProjectsWebApp.Areas.User.Controllers
{
    [Area("User")]
    [ApiController]
    [Route("api/scenes/{sceneId:int}/markers")]
    [AllowAnonymous] // allow unauthenticated access if you want the public tool to work without login
    public class MarkersController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IHubContext<StoryboardHub> _hub;

        public MarkersController(ApplicationDbContext context, IHubContext<StoryboardHub> hub)
        {
            _context = context;
            _hub = hub;
        }

        // GET: api/scenes/{sceneId}/markers
        [HttpGet]
        public async Task<IActionResult> List(int sceneId)
        {
            var exists = await _context.Scenes.AnyAsync(s => s.Id == sceneId);
            if (!exists) return NotFound();

            var markers = await _context.Markers
                .Where(m => m.SceneId == sceneId)
                .OrderBy(m => m.Number).ThenBy(m => m.Id)
                .Select(m => new
                {
                    m.Id,
                    m.X,
                    m.Y,
                    m.Number,
                    m.ColorHex,
                    m.Description,
                    m.Ziel,
                    m.Datenablage,
                    m.Quellen,
                    m.PromptIdee,
                    m.Reflexion,
                    m.Model,
                    m.Taxonomie,
                    m.SceneId
                })
                .ToListAsync();

            return Ok(markers);
        }

        public class CreateDto
        {
            public double X { get; set; }
            public double Y { get; set; }
            public string? ColorHex { get; set; }
        }

        // POST: api/scenes/{sceneId}/markers
        [HttpPost]
        public async Task<IActionResult> Create(int sceneId, [FromBody] CreateDto dto)
        {
            var scene = await _context.Scenes
                .Include(s => s.Storyboard)
                .FirstOrDefaultAsync(s => s.Id == sceneId);
            if (scene == null) return NotFound();

            // Authorization: only owner (login), anon owner cookie, or valid edit cookie may write
            var canWrite = false;
            var uid = User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (!string.IsNullOrWhiteSpace(scene.Storyboard?.OwnerId) && uid == scene.Storyboard.OwnerId)
            {
                canWrite = true;
            }
            else if (!string.IsNullOrWhiteSpace(scene.Storyboard?.OwnerTokenHash)
                     && Request.Cookies.TryGetValue("sb_uid", out var anonTok)
                     && !string.IsNullOrWhiteSpace(anonTok))
            {
                var anonHash = System.Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(anonTok))).ToLowerInvariant();
                if (string.Equals(anonHash, scene.Storyboard!.OwnerTokenHash, System.StringComparison.OrdinalIgnoreCase))
                    canWrite = true;
            }
            if (!canWrite && !string.IsNullOrWhiteSpace(scene.Storyboard?.PublicId))
            {
                var slug = scene.Storyboard.PublicId;
                if (Request.Cookies.TryGetValue($"sbedit_{slug}", out var editPlain) && !string.IsNullOrWhiteSpace(editPlain))
                {
                    var editHash = System.Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(editPlain))).ToLowerInvariant();
                    if (string.Equals(editHash, scene.Storyboard!.EditKeyHash, System.StringComparison.OrdinalIgnoreCase))
                        canWrite = true;
                }
            }
            if (!canWrite) return Forbid();

            var nextNumber = await _context.Markers
                                   .Where(m => m.SceneId == sceneId)
                                   .Select(m => (int?)m.Number)
                                   .MaxAsync() ?? 0;

            var m = new Marker
            {
                SceneId = sceneId,
                X = Math.Clamp(dto.X, 0, 1),
                Y = Math.Clamp(dto.Y, 0, 1),
                Number = nextNumber + 1,
                ColorHex = string.IsNullOrWhiteSpace(dto.ColorHex) ? "#78a7ff" : dto.ColorHex.Trim(),
                Taxonomie = scene.Storyboard?.Taxonomie // default marker level to storyboard's max (can be lowered later)
            };

            _context.Markers.Add(m);
            await _context.SaveChangesAsync();

            var payload = new
            {
                m.Id,
                m.X,
                m.Y,
                m.Number,
                m.ColorHex,
                m.Description,
                m.Ziel,
                m.Datenablage,
                m.Quellen,
                m.PromptIdee,
                m.Reflexion,
                m.Model,
                m.Taxonomie,
                m.SceneId
            };
            await _hub.Clients.Group($"scene-{sceneId}").SendAsync("MarkerCreated", payload);

            return CreatedAtAction(nameof(List), new { sceneId }, payload);
        }

        public class UpdateDto
        {
            public double? X { get; set; }
            public double? Y { get; set; }
            public int? Number { get; set; }
            public string? ColorHex { get; set; }
            public string? Description { get; set; }
            public string? Ziel { get; set; }
            public string? Datenablage { get; set; }
            public string? Quellen { get; set; }
            public string? PromptIdee { get; set; }
            public string? Reflexion { get; set; }
            public string? Model { get; set; }
            public TaxonomieStufe? Taxonomie { get; set; }
        }

        // PUT: api/scenes/{sceneId}/markers/{id}
        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int sceneId, int id, [FromBody] UpdateDto dto)
        {
            var m = await _context.Markers
                .Include(x => x.Scene)
                .ThenInclude(sc => sc.Storyboard)
                .FirstOrDefaultAsync(x => x.Id == id && x.SceneId == sceneId);
            if (m == null) return NotFound();

            // Authorization check against owning storyboard
            var canWrite = false;
            var sb = m.Scene?.Storyboard;
            var uid = User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (!string.IsNullOrWhiteSpace(sb?.OwnerId) && uid == sb.OwnerId)
            {
                canWrite = true;
            }
            else if (!string.IsNullOrWhiteSpace(sb?.OwnerTokenHash)
                     && Request.Cookies.TryGetValue("sb_uid", out var anonTok)
                     && !string.IsNullOrWhiteSpace(anonTok))
            {
                var anonHash = System.Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(anonTok))).ToLowerInvariant();
                if (string.Equals(anonHash, sb!.OwnerTokenHash, System.StringComparison.OrdinalIgnoreCase))
                    canWrite = true;
            }
            if (!canWrite && !string.IsNullOrWhiteSpace(sb?.PublicId))
            {
                var slug = sb.PublicId;
                if (Request.Cookies.TryGetValue($"sbedit_{slug}", out var editPlain) && !string.IsNullOrWhiteSpace(editPlain))
                {
                    var editHash = System.Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(editPlain))).ToLowerInvariant();
                    if (string.Equals(editHash, sb!.EditKeyHash, System.StringComparison.OrdinalIgnoreCase))
                        canWrite = true;
                }
            }
            if (!canWrite) return Forbid();

            if (dto.X.HasValue) m.X = Math.Clamp(Math.Round(dto.X.Value, 4), 0, 1);
            if (dto.Y.HasValue) m.Y = Math.Clamp(Math.Round(dto.Y.Value, 4), 0, 1);
            if (dto.Number.HasValue) m.Number = dto.Number.Value;
            if (dto.ColorHex != null) m.ColorHex = dto.ColorHex.Trim();
            if (dto.Description != null) m.Description = dto.Description.Trim();
            if (dto.Ziel != null) m.Ziel = dto.Ziel.Trim();
            if (dto.Datenablage != null) m.Datenablage = dto.Datenablage.Trim();
            if (dto.Quellen != null) m.Quellen = dto.Quellen.Trim();
            if (dto.PromptIdee != null) m.PromptIdee = dto.PromptIdee.Trim();
            if (dto.Reflexion != null) m.Reflexion = dto.Reflexion.Trim();
            if (dto.Model != null) m.Model = dto.Model.Trim();
            if (dto.Taxonomie.HasValue)
            {
                var max = m.Scene?.Storyboard?.Taxonomie;
                var requested = dto.Taxonomie.Value;
                // clamp to storyboard max if set
                if (max.HasValue && requested > max.Value) m.Taxonomie = max.Value; else m.Taxonomie = requested;
            }

            await _context.SaveChangesAsync();

            var updated = new
            {
                m.Id,
                m.X,
                m.Y,
                m.Number,
                m.ColorHex,
                m.Description,
                m.Ziel,
                m.Datenablage,
                m.Quellen,
                m.PromptIdee,
                m.Reflexion,
                m.Model,
                m.Taxonomie,
                m.SceneId
            };
            await _hub.Clients.Group($"scene-{sceneId}").SendAsync("MarkerUpdated", updated);
            return Ok(updated);
        }

        // DELETE: api/scenes/{sceneId}/markers/{id}
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int sceneId, int id)
        {
            var m = await _context.Markers
                .Include(x => x.Scene)
                .ThenInclude(sc => sc.Storyboard)
                .FirstOrDefaultAsync(x => x.Id == id && x.SceneId == sceneId);
            if (m == null) return NotFound();

            // Authorization check against owning storyboard
            var canWrite = false;
            var sb = m.Scene?.Storyboard;
            var uid = User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (!string.IsNullOrWhiteSpace(sb?.OwnerId) && uid == sb.OwnerId)
            {
                canWrite = true;
            }
            else if (!string.IsNullOrWhiteSpace(sb?.OwnerTokenHash)
                     && Request.Cookies.TryGetValue("sb_uid", out var anonTok)
                     && !string.IsNullOrWhiteSpace(anonTok))
            {
                var anonHash = System.Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(anonTok))).ToLowerInvariant();
                if (string.Equals(anonHash, sb!.OwnerTokenHash, System.StringComparison.OrdinalIgnoreCase))
                    canWrite = true;
            }
            if (!canWrite && !string.IsNullOrWhiteSpace(sb?.PublicId))
            {
                var slug = sb.PublicId;
                if (Request.Cookies.TryGetValue($"sbedit_{slug}", out var editPlain) && !string.IsNullOrWhiteSpace(editPlain))
                {
                    var editHash = System.Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(editPlain))).ToLowerInvariant();
                    if (string.Equals(editHash, sb!.EditKeyHash, System.StringComparison.OrdinalIgnoreCase))
                        canWrite = true;
                }
            }
            if (!canWrite) return Forbid();

            _context.Markers.Remove(m);
            await _context.SaveChangesAsync();

            // Renumber remaining markers so numbering stays contiguous (1..N).
            var remaining = await _context.Markers
                .Where(x => x.SceneId == sceneId)
                .OrderBy(x => x.Number).ThenBy(x => x.Id)
                .ToListAsync();
            for (var i = 0; i < remaining.Count; i++)
            {
                var newNumber = i + 1;
                if (remaining[i].Number != newNumber) remaining[i].Number = newNumber;
            }
            await _context.SaveChangesAsync();

            var order = remaining.Select(x => new { id = x.Id, number = x.Number }).ToList();
            await _hub.Clients.Group($"scene-{sceneId}").SendAsync("MarkerDeleted", new { id, sceneId, order });
            return Ok(new { id, sceneId, order });
        }

        // ---- Authorization helpers (mirror ScenesController) ----
        private const string AnonCookie = "sb_uid";
        private static string EditCookieName(string slug) => $"sbedit_{slug}";

        private static string Sha256Hex(string value)
        {
            var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(value));
            return Convert.ToHexString(bytes).ToLowerInvariant();
        }

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
        {
            var uid = User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return !string.IsNullOrWhiteSpace(sb.OwnerId) && string.Equals(sb.OwnerId, uid, StringComparison.Ordinal);
        }

        private bool CanWrite(Storyboard sb) => IsStaff || IsOwnerByLogin(sb) || IsOwnerByToken(sb) || HasEditCookie(sb);

        // PATCH /Markers/{id}
        // Partial update for inline marker edits. null = leave unchanged.
        // Uses a root-level route (leading '/') to override the class-level api prefix.
        [AllowAnonymous]
        [HttpPatch("/Markers/{id:int}")]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> Patch(int id, [FromBody] MarkerPatchDto dto)
        {
            if (dto == null) return BadRequest();

            var m = await _context.Markers
                .Include(x => x.Scene)
                .ThenInclude(s => s!.Storyboard)
                .FirstOrDefaultAsync(x => x.Id == id);
            if (m == null) return NotFound();
            if (m.Scene?.Storyboard == null) return NotFound();
            if (!CanWrite(m.Scene.Storyboard)) return Forbid();

            if (dto.RowVersion != null && m.RowVersion != null &&
                !dto.RowVersion.SequenceEqual(m.RowVersion))
            {
                return Conflict(new { reason = "stale", rowVersion = m.RowVersion });
            }

            if (dto.X is not null) m.X = dto.X.Value;
            if (dto.Y is not null) m.Y = dto.Y.Value;
            if (dto.Number is not null) m.Number = dto.Number.Value;
            if (dto.ColorHex is not null) m.ColorHex = dto.ColorHex;
            if (dto.Description is not null) m.Description = dto.Description;
            if (dto.Ziel is not null) m.Ziel = dto.Ziel;
            if (dto.Datenablage is not null) m.Datenablage = dto.Datenablage;
            if (dto.Quellen is not null) m.Quellen = dto.Quellen;
            if (dto.PromptIdee is not null) m.PromptIdee = dto.PromptIdee;
            if (dto.Reflexion is not null) m.Reflexion = dto.Reflexion;
            if (dto.Model is not null) m.Model = dto.Model;
            if (dto.Taxonomie is not null) m.Taxonomie = dto.Taxonomie;

            try { await _context.SaveChangesAsync(); }
            catch (DbUpdateConcurrencyException) { return Conflict(new { reason = "stale" }); }

            return Ok(new { ok = true, rowVersion = m.RowVersion });
        }
    }
}
