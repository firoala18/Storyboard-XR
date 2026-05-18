using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.SignalR;
using ProjectsWebApp.DataAccsess.Data;
using ProjectsWebApp.Models;

namespace ProjectsWebApp.Hubs
{
    public class StoryboardHub : Hub
    {
        // Single-process in-memory presence index. Keyed by storyboard id, then by
        // connection id. If we ever scale out, swap this for a Redis backplane —
        // the hub surface stays the same.
        private static readonly ConcurrentDictionary<int, ConcurrentDictionary<string, PresenceEntry>> _presence
            = new();

        private static readonly string[] _palette =
        {
            "#ef4444", "#f59e0b", "#10b981", "#3b82f6", "#8b5cf6",
            "#ec4899", "#0ea5e9", "#84cc16", "#f97316", "#14b8a6"
        };

        private readonly ApplicationDbContext _db;

        public StoryboardHub(ApplicationDbContext db) { _db = db; }

        // ─── Existing group helpers (unchanged surface) ──────────────────
        public Task JoinScene(int sceneId)
            => Groups.AddToGroupAsync(Context.ConnectionId, $"scene-{sceneId}");

        public Task LeaveScene(int sceneId)
            => Groups.RemoveFromGroupAsync(Context.ConnectionId, $"scene-{sceneId}");

        public Task JoinStoryboard(int storyboardId)
            => Groups.AddToGroupAsync(Context.ConnectionId, $"sb-{storyboardId}");

        public Task LeaveStoryboard(int storyboardId)
            => Groups.RemoveFromGroupAsync(Context.ConnectionId, $"sb-{storyboardId}");

        // ─── Presence + cursors ──────────────────────────────────────────
        public async Task<PresenceEntry?> JoinPresence(int storyboardId)
        {
            var sb = await _db.Storyboards.FindAsync(storyboardId);
            if (sb == null || !sb.IsShared) return null;

            var role = ComputeRole(sb);
            if (role == null) return null;

            var (userKey, displayName, initials) = ResolveIdentity();
            var entry = new PresenceEntry
            {
                ConnectionId = Context.ConnectionId,
                UserKey = userKey,
                DisplayName = displayName,
                Initials = initials,
                Color = PickColor(userKey),
                Role = role
            };

            var sbMap = _presence.GetOrAdd(storyboardId, _ => new ConcurrentDictionary<string, PresenceEntry>());
            sbMap[Context.ConnectionId] = entry;

            await Groups.AddToGroupAsync(Context.ConnectionId, $"sb-{storyboardId}");
            Context.Items["presence-storyboard"] = storyboardId;
            Context.Items["presence-role"] = role;

            await Clients.OthersInGroup($"sb-{storyboardId}").SendAsync("PresenceJoined", entry);
            return entry;
        }

        public List<PresenceEntry> PresenceHere(int storyboardId)
        {
            if (!_presence.TryGetValue(storyboardId, out var map)) return new List<PresenceEntry>();
            return map.Values
                .Where(e => e.ConnectionId != Context.ConnectionId)
                .ToList();
        }

        public Task CursorMove(int storyboardId, CursorPayload payload)
        {
            if (payload == null) return Task.CompletedTask;
            if (Context.Items["presence-role"] is not string role || role != "editor")
                return Task.CompletedTask;

            // Server stamps the connectionId so clients can't impersonate.
            var msg = new
            {
                connectionId = Context.ConnectionId,
                mode = payload.Mode,
                x = payload.X,
                y = payload.Y,
                dx = payload.Dx,
                dy = payload.Dy,
                anchor = payload.Anchor,
                sceneId = payload.SceneId
            };
            return Clients.OthersInGroup($"sb-{storyboardId}").SendAsync("CursorMove", msg);
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            if (Context.Items["presence-storyboard"] is int sbid
                && _presence.TryGetValue(sbid, out var map)
                && map.TryRemove(Context.ConnectionId, out _))
            {
                await Clients.Group($"sb-{sbid}")
                    .SendAsync("PresenceLeft", new { connectionId = Context.ConnectionId });
            }
            await base.OnDisconnectedAsync(exception);
        }

        // ─── Internals ───────────────────────────────────────────────────
        private string? ComputeRole(Storyboard sb)
        {
            var http = Context.GetHttpContext();

            // Owner by login
            var uid = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!string.IsNullOrWhiteSpace(sb.OwnerId) && string.Equals(uid, sb.OwnerId, StringComparison.Ordinal))
                return "editor";

            // Owner by anon token cookie
            if (http != null
                && !string.IsNullOrWhiteSpace(sb.OwnerTokenHash)
                && http.Request.Cookies.TryGetValue("sb_uid", out var tok)
                && !string.IsNullOrWhiteSpace(tok)
                && string.Equals(Sha256Hex(tok), sb.OwnerTokenHash, StringComparison.OrdinalIgnoreCase))
                return "editor";

            // Edit-cookie holder
            if (http != null
                && !string.IsNullOrWhiteSpace(sb.PublicId)
                && http.Request.Cookies.TryGetValue($"sbedit_{sb.PublicId}", out var ek)
                && !string.IsNullOrWhiteSpace(ek)
                && string.Equals(Sha256Hex(ek), sb.EditKeyHash, StringComparison.OrdinalIgnoreCase))
                return "editor";

            // Shared editing (authenticated when storyboard has an owner)
            if (sb.AllowSharedEditing
                && (string.IsNullOrWhiteSpace(sb.OwnerId) || Context.User?.Identity?.IsAuthenticated == true))
                return "editor";

            // Otherwise viewer if the link is shared
            return sb.IsShared ? "viewer" : null;
        }

        private (string userKey, string displayName, string initials) ResolveIdentity()
        {
            // Authenticated → use NameIdentifier as stable key, UserName as display name
            var uid = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!string.IsNullOrWhiteSpace(uid))
            {
                var name = Context.User?.Identity?.Name ?? Context.User?.FindFirst(ClaimTypes.Email)?.Value ?? "User";
                return (uid, FriendlyName(name), Initials(name));
            }

            // Anonymous owner (sb_uid cookie) → derive a stable short key from the token hash
            var http = Context.GetHttpContext();
            if (http != null && http.Request.Cookies.TryGetValue("sb_uid", out var tok) && !string.IsNullOrWhiteSpace(tok))
            {
                var hash = Sha256Hex(tok);
                return ("anon:" + hash[..8], "Gast " + hash[..4].ToUpperInvariant(), hash[..2].ToUpperInvariant());
            }

            // Read-only public viewer: identity scoped to this connection only
            var c = Context.ConnectionId;
            return ("conn:" + c, "Gast", c.Length >= 2 ? c[..2].ToUpperInvariant() : "GA");
        }

        private static string FriendlyName(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return "User";
            var at = raw.IndexOf('@');
            var local = at > 0 ? raw[..at] : raw;
            return local.Length > 0 ? local : raw;
        }

        private static string Initials(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return "?";
            var at = raw.IndexOf('@');
            var local = at > 0 ? raw[..at] : raw;
            var parts = local.Split(new[] { '.', '-', '_', ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length >= 2)
                return (char.ToUpperInvariant(parts[0][0]).ToString() + char.ToUpperInvariant(parts[^1][0])).Trim();
            return (local.Length >= 2 ? local[..2] : (local + "?")[..2]).ToUpperInvariant();
        }

        private static string PickColor(string key)
        {
            unchecked
            {
                var h = 2166136261u;
                foreach (var ch in key) { h ^= ch; h *= 16777619u; }
                return _palette[(int)(h % (uint)_palette.Length)];
            }
        }

        private static string Sha256Hex(string value)
            => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();
    }

    public class PresenceEntry
    {
        public string ConnectionId { get; set; } = "";
        public string UserKey { get; set; } = "";
        public string DisplayName { get; set; } = "";
        public string Initials { get; set; } = "";
        public string Color { get; set; } = "";
        // "editor" | "viewer"
        public string Role { get; set; } = "";
    }

    public class CursorPayload
    {
        // "scene" | "anchor" | "page" | "away"
        public string Mode { get; set; } = "";
        // scene mode: normalized 0..1
        public double? X { get; set; }
        public double? Y { get; set; }
        // anchor mode: pixel offset inside the named element
        public double? Dx { get; set; }
        public double? Dy { get; set; }
        public string? Anchor { get; set; }
        public int? SceneId { get; set; }
    }
}
