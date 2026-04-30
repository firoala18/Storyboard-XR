using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.SignalR;
using ProjectsWebApp.DataAccsess.Data;
using ProjectsWebApp.DataAccsess.Repository.IRepository;
using ProjectsWebApp.DataAccsess.Repository.IRepository.Classes;
using ProjectsWebApp.Models;
using ProjectsWebApp.Utility;
using System;
using System.Globalization;
using System.Net;
using System.IO;
using System.Threading.RateLimiting;
// optional if implicit usings are off
using System.Linq;

var builder = WebApplication.CreateBuilder(args);

// ─────────── App identity (Storyboard) ───────────
var appBasePath = Environment.GetEnvironmentVariable("ASPNETCORE_PATHBASE")?.TrimEnd('/')
                   ?? "/apps/storyboard";   // external mount path
var appName = "Storyboard";            // unique per app
var cookiePrefix = "Storyboard";            // base for all cookie names

// ─────────── Session ───────────
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;

    options.Cookie.Name = $"{cookiePrefix}.Session";
    options.Cookie.Path = appBasePath;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.Lax;
});

// ─────────── MVC, Razor, Localization ───────────
builder.Services
    .AddControllersWithViews()
    .AddViewLocalization(LanguageViewLocationExpanderFormat.Suffix)
    .AddDataAnnotationsLocalization();

builder.Services.AddRazorPages();

// Real-time
builder.Services.AddSignalR();

// ─────────── Form-Limits ───────────
builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 500_000_000; // 500 MB
});

// ─────────── DI ───────────
builder.Services.AddSingleton<IActionContextAccessor, ActionContextAccessor>();
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped<IEmailSender, EmailSender>();
//builder.Services.AddScoped<IContactEmailService, ContactEmailService>();
//builder.Services.AddSingleton<IPromptFilterAiService, PromptFilterAiService>();

// ─────────── EF Core (PostgreSQL via Npgsql) ───────────
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"))
);

// ─────────── Identity with Roles ───────────
builder.Services
    .AddIdentity<IdentityUser, IdentityRole>(options =>
    {
        // Password & lockout hardening
        options.Password.RequiredLength = 10;
        options.Password.RequireDigit = true;
        options.Password.RequireUppercase = true;
        options.Password.RequireLowercase = true;
        options.Password.RequireNonAlphanumeric = true;

        options.Lockout.AllowedForNewUsers = true;
        options.Lockout.MaxFailedAccessAttempts = 5; // 5 bad attempts
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5); // 5 minutes lockout
    })
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

// Login/Logout routes + secure cookies behind TLS proxy (unique + scoped)
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Identity/Account/Login";
    options.LogoutPath = "/Identity/Account/Logout";
    options.AccessDeniedPath = "/Identity/Account/AccessDenied";

    options.Cookie.Name = $"{cookiePrefix}.Identity";
    options.Cookie.Path = appBasePath;                 // only visible under /apps/storyboard
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.SlidingExpiration = true;
});

// ─────────── Antiforgery (unique + scoped) ───────────
// Cookie.Path must be a real path, not empty. Empty string serializes as
// "Path=" which browsers treat as the request URI's default-path — meaning
// a token cookie set on /s/my would scope to /s and never be sent to a POST
// at /User/Storyboards/... So when appBasePath is "" (dev), fall back to "/".
var cookiePath = string.IsNullOrEmpty(appBasePath) ? "/" : appBasePath;

builder.Services.AddAntiforgery(o =>
{
    o.Cookie.Name = $"{cookiePrefix}.AntiForgery";
    o.Cookie.Path = cookiePath;
    o.Cookie.HttpOnly = true;
    o.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    o.SuppressXFrameOptionsHeader = false;
});

// ─────────── Data Protection (isolate from other apps) ───────────
builder.Services.AddDataProtection();

// OS-aware DataProtection key storage (configurable)
var dpKeysPath = builder.Configuration["DataProtection:KeysPath"];
if (string.IsNullOrWhiteSpace(dpKeysPath))
{
    if (OperatingSystem.IsWindows())
    {
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        dpKeysPath = Path.Combine(localAppData, "aspnet-dpkeys", "storyboard");
    }
    else
    {
        dpKeysPath = "/var/aspnet-dpkeys/storyboard";
    }
}
Directory.CreateDirectory(dpKeysPath);

builder.Services.AddDataProtection()
    .SetApplicationName(appName)
    .PersistKeysToFileSystem(new DirectoryInfo(dpKeysPath));

// ─────────── Policies ───────────
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy =>
        policy.RequireRole("Admin", "SuperAdmin"));
});

// ─────────── Rate limiting (login throttling) ───────────
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    // Global limiter: only active on POST /Identity/Account/Login, keyed by remote IP
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
    {
        var path = httpContext.Request.Path.Value ?? string.Empty;
        var method = httpContext.Request.Method ?? string.Empty;

        // Only throttle login POSTs; allow GET /Identity/Account/Login to always render the form
        if (!path.StartsWith("/Identity/Account/Login", StringComparison.OrdinalIgnoreCase)
            || !HttpMethods.IsPost(method))
        {
            return RateLimitPartition.GetNoLimiter("nolimit");
        }

        var ip = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        return RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: ip,
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 5, // 5 login attempts
                Window = TimeSpan.FromMinutes(5),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0
            });
    });
});

// ─────────── Localization cultures ───────────
var supported = new[] { new CultureInfo("de-DE"), new CultureInfo("en-US") };
builder.Services.AddLocalization(opts => opts.ResourcesPath = "Resources");

AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

var app = builder.Build();

app.UseRequestLocalization(new RequestLocalizationOptions
{
    DefaultRequestCulture = new RequestCulture("de-DE"),
    SupportedCultures = supported,
    SupportedUICultures = supported
});

// ─────────── Middleware ───────────
app.UseSession();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

// Respect reverse proxy (Apache) for scheme/host so redirects are correct
app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto,
    // Accept from local reverse proxy:
    KnownProxies = { IPAddress.Parse("127.0.0.1"), IPAddress.IPv6Loopback }
});

app.UseHttpsRedirection();

// Ensure the app runs under the expected base path (sub-app)
if (!string.IsNullOrEmpty(appBasePath) && appBasePath != "/")
{
    app.UsePathBase(appBasePath);
}

// Serve static files with extended MIME types for H5P content
var staticFileProvider = new Microsoft.AspNetCore.StaticFiles.FileExtensionContentTypeProvider();
staticFileProvider.Mappings[".h5p"] = "application/zip";
if (!staticFileProvider.Mappings.ContainsKey(".json"))
    staticFileProvider.Mappings[".json"] = "application/json";
app.UseStaticFiles(new StaticFileOptions
{
    ContentTypeProvider = staticFileProvider,
    ServeUnknownFileTypes = true,
    DefaultContentType = "application/octet-stream"
});

app.UseRouting();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

// SignalR hub endpoint (respecting base path)
app.MapHub<ProjectsWebApp.Hubs.StoryboardHub>("/hubs/storyboard");

app.MapRazorPages();
app.MapControllerRoute(
    name: "default",
    pattern: "{area=user}/{controller=Home}/{action=Home}/{id?}"
);

// ─────────── Seed roles & default accounts ───────────
using (var scope = app.Services.CreateScope())
{
    var roleM = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    var userM = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();

    // 1) Rollen anlegen
    string[] roles = { "User", "Admin", "SuperAdmin" };
    foreach (var r in roles)
    {
        if (!await roleM.RoleExistsAsync(r))
            await roleM.CreateAsync(new IdentityRole(r));
    }

    // 2) Optionale SuperAdmin-Anlage per Umgebungsvariablen
    // Setzen Sie SUPERADMIN_EMAIL und SUPERADMIN_PASSWORD in Ihrer Umgebung/CI.
    var seedEmail = Environment.GetEnvironmentVariable("SUPERADMIN_EMAIL");
    var seedPwd = Environment.GetEnvironmentVariable("SUPERADMIN_PASSWORD");

    if (!string.IsNullOrWhiteSpace(seedEmail) && !string.IsNullOrWhiteSpace(seedPwd))
    {
        var u = await userM.FindByEmailAsync(seedEmail);
        if (u == null)
        {
            u = new IdentityUser
            {
                UserName = seedEmail,
                Email = seedEmail,
                EmailConfirmed = true
            };
            await userM.CreateAsync(u, seedPwd);
        }

        if (!await userM.IsInRoleAsync(u, "Admin"))
            await userM.AddToRoleAsync(u, "Admin");
        if (!await userM.IsInRoleAsync(u, "SuperAdmin"))
            await userM.AddToRoleAsync(u, "SuperAdmin");
    }
}

// ─────────── Seed initial data ───────────
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

    if (!db.RegistrationCodes.Any())
    {
        db.RegistrationCodes.AddRange(
            new RegistrationCode { Code = "ABC123" },
            new RegistrationCode { Code = "XYZ789" },
            new RegistrationCode { Code = "WELCOME2025" }
        );
        await db.SaveChangesAsync();
    }
}

app.Run();

// Minimal hub class (inlined to avoid creating a new file here)
namespace ProjectsWebApp.Hubs
{
    public class StoryboardHub : Hub
    {
        public Task JoinScene(int sceneId)
            => Groups.AddToGroupAsync(Context.ConnectionId, $"scene-{sceneId}");

        public Task LeaveScene(int sceneId)
            => Groups.RemoveFromGroupAsync(Context.ConnectionId, $"scene-{sceneId}");

        public Task JoinStoryboard(int storyboardId)
            => Groups.AddToGroupAsync(Context.ConnectionId, $"sb-{storyboardId}");

        public Task LeaveStoryboard(int storyboardId)
            => Groups.RemoveFromGroupAsync(Context.ConnectionId, $"sb-{storyboardId}");
    }
}
