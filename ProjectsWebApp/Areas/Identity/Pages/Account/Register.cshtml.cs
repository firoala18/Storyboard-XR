using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using ProjectsWebApp.DataAccsess.Data;
using ProjectsWebApp.Models;
using ProjectsWebApp.Utility;

namespace ProjectsWebApp.Areas.Identity.Pages.Account
{
    [AllowAnonymous]
    public class RegisterModel : PageModel
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IUserStore<IdentityUser> _userStore;
        private readonly IUserEmailStore<IdentityUser> _emailStore;
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly ILogger<RegisterModel> _logger;
        //private readonly IEmailSender _emailSender;
        [TempData] public string? RegisterSuccess { get; set; }

        public RegisterModel(
            ApplicationDbContext db,
            UserManager<IdentityUser> userManager,
            IUserStore<IdentityUser> userStore,
            SignInManager<IdentityUser> signInManager,
            ILogger<RegisterModel> logger)
        {
            _db = db;
            _userManager = userManager;
            _userStore = userStore;
            _emailStore = GetEmailStore();
            _signInManager = signInManager;
            _logger = logger;
           
        }

        [BindProperty]
        public InputModel Input { get; set; }

        public string ReturnUrl { get; set; }
        public IList<AuthenticationScheme> ExternalLogins { get; set; }

        public class InputModel
        {
            [Required]
            [Display(Name = "Einladungscode")]
            public string InviteCode { get; set; }

            [Required]
            [Display(Name = "Name")]
            public string Name { get; set; }

            [Required]
            [EmailAddress]
            [Display(Name = "E-Mail")]
            public string Email { get; set; }

            [Required]
            [StringLength(100, MinimumLength = 6, ErrorMessage = "{0} muss zwischen {2} und {1} Zeichen lang sein.")]
            [DataType(DataType.Password)]
            [Display(Name = "Passwort")]
            public string Password { get; set; }

            [DataType(DataType.Password)]
            [Compare("Password", ErrorMessage = "Die Passwörter stimmen nicht überein.")]
            [Display(Name = "Passwort bestätigen")]
            public string ConfirmPassword { get; set; }
        }

        public async Task OnGetAsync(string returnUrl = null)
        {
            ReturnUrl = returnUrl;
            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();
        }

        public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
        {
            // Always send the user *back* to the login page afterwards
            // ( Url.Content adds PathBase automatically  ->  /promptcoach/… in prod )
            returnUrl ??= Url.Content("~/Identity/Account/Login");

            ExternalLogins = (await _signInManager
                                  .GetExternalAuthenticationSchemesAsync())
                                  .ToList();

            /* ──────────────────────────────────────────
               GLOBAL try‑catch so *every* exception is
               logged and we still reach a clean result.
            ────────────────────────────────────────── */
            try
            {
                /* 1)  Model check */
                if (!ModelState.IsValid)
                    return Page();

                /* 2)  Invite‑code */
                var codeEntry = await _db.RegistrationCodes
                                         .FirstOrDefaultAsync(c => c.Code == Input.InviteCode &&
                                                                   c.IsActive);
                if (codeEntry is null)
                {
                    TempData["error"] = "Ungültiger Einladungscode.";
                    return RedirectToPage(new { returnUrl });
                }

                /* 3)  Create user */
                var user = new AplicationUser
                {
                    Name = Input.Name,
                    Email = Input.Email,
                    UserName = Input.Email
                };

                var result = await _userManager.CreateAsync(user, Input.Password);
                if (!result.Succeeded)
                {
                    foreach (var err in result.Errors)
                        ModelState.AddModelError(string.Empty, err.Description);
                    return Page();                       //  ← validation error, no 500
                }

                /* 4)  consume invite + role */
                //codeEntry.IsActive = false;
                await _db.SaveChangesAsync();
                await _userManager.AddToRoleAsync(user, SD.Role_Customer);

                /* 5)  Success */
                TempData["success"] =
                    "Registrierung erfolgreich – bitte loggen Sie sich ein.";
                return LocalRedirect(returnUrl);
            }
            catch (Exception ex)
            {
                /* ◂────── LOG EVERYTHING ──────▸ */
                _logger.LogError(ex,
                    "Fehler in Register.OnPostAsync – User {Email}", Input?.Email);

                /* Optional: more detail for *your* log target
                 * e.g.  await _db.ErrorLog.AddAsync(new ErrorLog { … });
                 *       await _db.SaveChangesAsync();
                 */

                /* Friendly error screen, HTTP 500 */
                Response.StatusCode = StatusCodes.Status500InternalServerError;
                // ‑‑ statt View("Error")
                return Redirect("~/Identity/Account/Login");
                // Razor Page /Views/Shared/Error.cshtml
            }
        }



        private IUserEmailStore<IdentityUser> GetEmailStore()
        {
            if (!_userManager.SupportsUserEmail)
                throw new NotSupportedException("UserStore muss E-Mail unterstützen.");
            return (IUserEmailStore<IdentityUser>)_userStore;
        }
    }
}
