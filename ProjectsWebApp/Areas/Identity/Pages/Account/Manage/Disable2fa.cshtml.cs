using System;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;

namespace ProjectsWebApp.Areas.Identity.Pages.Account.Manage
{
    public class Disable2faModel : PageModel
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly ILogger<Disable2faModel> _logger;

        public Disable2faModel(
            UserManager<IdentityUser> userManager,
            ILogger<Disable2faModel> logger)
        {
            _userManager = userManager;
            _logger = logger;
        }

        [TempData]
        public string StatusMessage { get; set; }

        [BindProperty]
        public InputModel Input { get; set; }

        public class InputModel
        {
            [Required(ErrorMessage = "Verifizierungscode ist erforderlich.")]
            [StringLength(7, MinimumLength = 6)]
            [DataType(DataType.Text)]
            [Display(Name = "Verifizierungscode")]
            public string Code { get; set; }
        }

        public async Task<IActionResult> OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return NotFound($"Benutzer konnte nicht geladen werden.");

            if (!await _userManager.GetTwoFactorEnabledAsync(user))
                throw new InvalidOperationException("2FA ist aktuell nicht aktiviert.");

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
                return Page();

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return NotFound($"Benutzer konnte nicht geladen werden.");

            // Code verifizieren
            var tokenValid = await _userManager.VerifyTwoFactorTokenAsync(
                user,
                _userManager.Options.Tokens.AuthenticatorTokenProvider,
                Input.Code.Replace(" ", "").Replace("-", ""));

            if (!tokenValid)
            {
                ModelState.AddModelError(nameof(Input.Code), "Ungültiger Verifizierungscode.");
                return Page();
            }

            // 2FA deaktivieren
            var result = await _userManager.SetTwoFactorEnabledAsync(user, false);
            if (!result.Succeeded)
                throw new InvalidOperationException("Fehler beim Deaktivieren der 2FA.");

            _logger.LogInformation("User {UserId} hat 2FA deaktiviert.", await _userManager.GetUserIdAsync(user));
            StatusMessage = "2FA wurde deaktiviert.";
            return RedirectToPage("./TwoFactorAuthentication");
        }
    }
}
