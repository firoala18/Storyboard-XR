using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ProjectsWebApp.Models;

namespace ProjectsWebApp.Areas.Identity.Pages.Account.Manage;

[Authorize(Roles = "SuperAdmin")]
public class ManageAdminsModel : PageModel
{
    private readonly UserManager<IdentityUser> _userMgr;
    private readonly SignInManager<IdentityUser> _signInMgr;

    public ManageAdminsModel(UserManager<IdentityUser> userMgr,
                             SignInManager<IdentityUser> signInMgr)
    {
        _userMgr = userMgr;
        _signInMgr = signInMgr;
    }

    public UserManager<IdentityUser> UserManager => _userMgr;

    [BindProperty] public string NewAdminEmailRegister { get; set; }
    [BindProperty] public string NewAdminName { get; set; }
    [BindProperty] public string NewAdminPassword { get; set; }
    [BindProperty] public string RemoveAdminEmail { get; set; }
    [BindProperty] public string UpdateStatusEmail { get; set; }
    [BindProperty] public string UpdateStatusRole { get; set; }

    [BindProperty] public string RemoveUserEmail { get; set; }

    public List<IdentityUser> AdminUsers { get; } = new();
    public List<IdentityUser> RegularUsers { get; } = new();

    private async Task<int> TotalAdminsAsync() =>
        (await _userMgr.GetUsersInRoleAsync("Admin")).Count +
        (await _userMgr.GetUsersInRoleAsync("SuperAdmin")).Count;

    public async Task<IActionResult> OnGetAsync()
    {
        // Avoid awaiting inside enumeration of _userMgr.Users (which keeps a data reader open)
        // Prefetch role memberships and materialize users first.
        var admins = await _userMgr.GetUsersInRoleAsync("Admin");
        var superAdmins = await _userMgr.GetUsersInRoleAsync("SuperAdmin");
        var adminIds = admins.Select(x => x.Id).ToHashSet();
        var superAdminIds = superAdmins.Select(x => x.Id).ToHashSet();

        var users = await _userMgr.Users.ToListAsync();
        foreach (var u in users)
        {
            if (adminIds.Contains(u.Id) || superAdminIds.Contains(u.Id))
                AdminUsers.Add(u);
            else
                RegularUsers.Add(u);
        }
        return Page();
    }

    public async Task<IActionResult> OnPostRegisterAdminAsync()
    {
        if (await _userMgr.FindByEmailAsync(NewAdminEmailRegister) != null)
        {
            TempData["error"] = "Benutzer existiert bereits.";
            return RedirectToPage();
        }

        var user = new AplicationUser
        {
            UserName = NewAdminEmailRegister,
            Email = NewAdminEmailRegister,
            Name = NewAdminName,
            EmailConfirmed = true
        };
        var res = await _userMgr.CreateAsync(user, NewAdminPassword);
        if (!res.Succeeded)
        {
            TempData["error"] = string.Join(", ", res.Errors.Select(e => e.Description));
            return RedirectToPage();
        }

        await _userMgr.AddToRoleAsync(user, "Admin");
        TempData["success"] = "Admin erfolgreich angelegt.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostRemoveAdminAsync()
    {
        var user = await _userMgr.FindByEmailAsync(RemoveAdminEmail);
        if (user == null)
        {
            TempData["error"] = "Benutzer nicht gefunden.";
            return RedirectToPage();
        }

        if (RemoveAdminEmail.Equals(User.Identity!.Name,
            StringComparison.OrdinalIgnoreCase))
        {
            TempData["error"] = "Sie können sich nicht selbst löschen.";
            return RedirectToPage();
        }

        if (await TotalAdminsAsync() <= 1)
        {
            TempData["error"] = "Mindestens ein Admin muss bleiben.";
            return RedirectToPage();
        }

        var del = await _userMgr.DeleteAsync(user);
        TempData[del.Succeeded ? "success" : "error"] =
            del.Succeeded ? "Administrator gelöscht."
                          : "Fehler: " + string.Join(", ", del.Errors.Select(e => e.Description));
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostUpdateStatusAsync()
    {
        if (string.IsNullOrWhiteSpace(UpdateStatusEmail) ||
            string.IsNullOrWhiteSpace(UpdateStatusRole))
        {
            TempData["error"] = "Ungültige Eingabe.";
            return RedirectToPage();
        }

        var user = await _userMgr.FindByEmailAsync(UpdateStatusEmail);
        if (user == null)
        {
            TempData["error"] = "Benutzer nicht gefunden.";
            return RedirectToPage();
        }

        if (await TotalAdminsAsync() == 1 &&
            (await _userMgr.IsInRoleAsync(user, "Admin") ||
             await _userMgr.IsInRoleAsync(user, "SuperAdmin")) &&
            UpdateStatusRole != "SuperAdmin")
        {
            TempData["error"] = "Mindestens ein Admin muss bleiben.";
            return RedirectToPage();
        }

        if (UpdateStatusRole == "SuperAdmin")
        {
            if (!await _userMgr.IsInRoleAsync(user, "Admin"))
                await _userMgr.AddToRoleAsync(user, "Admin");
            if (!await _userMgr.IsInRoleAsync(user, "SuperAdmin"))
                await _userMgr.AddToRoleAsync(user, "SuperAdmin");
        }
        else if (UpdateStatusRole == "Admin")
        {
            if (await _userMgr.IsInRoleAsync(user, "SuperAdmin"))
                await _userMgr.RemoveFromRoleAsync(user, "SuperAdmin");
            if (!await _userMgr.IsInRoleAsync(user, "Admin"))
                await _userMgr.AddToRoleAsync(user, "Admin");
        }
        else // User
        {
            // Entferne alle Admin-Rollen
            if (await _userMgr.IsInRoleAsync(user, "SuperAdmin"))
                await _userMgr.RemoveFromRoleAsync(user, "SuperAdmin");
            if (await _userMgr.IsInRoleAsync(user, "Admin"))
                await _userMgr.RemoveFromRoleAsync(user, "Admin");
        }

        await _userMgr.UpdateSecurityStampAsync(user);
        if (UpdateStatusEmail.Equals(User.Identity!.Name,
            StringComparison.OrdinalIgnoreCase))
        {
            await _signInMgr.RefreshSignInAsync(user);
        }

        TempData["success"] = "Rolle wurde aktualisiert.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostRemoveUserAsync()
    {
        if (string.IsNullOrWhiteSpace(RemoveUserEmail))
        {
            TempData["error"] = "Ungültige Eingabe.";
            return RedirectToPage();
        }

        var user = await _userMgr.FindByEmailAsync(RemoveUserEmail);
        if (user == null)
        {
            TempData["error"] = "Benutzer nicht gefunden.";
            return RedirectToPage();
        }

        var del = await _userMgr.DeleteAsync(user);
        if (del.Succeeded)
            TempData["success"] = $"Benutzer {RemoveUserEmail} gelöscht.";
        else
            TempData["error"] = "Fehler: " + string.Join(", ", del.Errors.Select(e => e.Description));

        return RedirectToPage();
    }
    public async Task<IActionResult> OnPostDeleteAllUsersAsync()
    {
        // lösche alle Nicht-Admin-User
        var toDelete = (await _userMgr.GetUsersInRoleAsync("Admin"))
                       .Concat(await _userMgr.GetUsersInRoleAsync("SuperAdmin"));
        var all = await _userMgr.Users.ToListAsync();
        var normals = all.Except(toDelete, new IdentityUserComparer()).ToList();

        foreach (var u in normals)
            await _userMgr.DeleteAsync(u);

        TempData["success"] = "Alle regulären Benutzer wurden gelöscht.";
        return RedirectToPage();
    }

    // Hilfsklasse zum Vergleichen
    private class IdentityUserComparer : IEqualityComparer<IdentityUser>
    {
        public bool Equals(IdentityUser x, IdentityUser y)
            => x?.Id == y?.Id;
        public int GetHashCode(IdentityUser obj) => obj.Id.GetHashCode();
    }
}
