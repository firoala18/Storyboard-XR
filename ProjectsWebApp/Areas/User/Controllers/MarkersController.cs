using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.SignalR;
using ProjectsWebApp.DataAccsess.Data;
using ProjectsWebApp.Models;
using ProjectsWebApp.Hubs;

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

            await _hub.Clients.Group($"scene-{sceneId}").SendAsync("MarkerDeleted", new { id, sceneId });
            return NoContent();
        }
    }
}
