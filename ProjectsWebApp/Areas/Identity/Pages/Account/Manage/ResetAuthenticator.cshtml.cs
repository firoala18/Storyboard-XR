using System;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;

namespace ProjectsWebApp.Areas.Identity.Pages.Account.Manage
{
    public class ResetAuthenticatorModel : PageModel
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly ILogger<ResetAuthenticatorModel> _logger;
        private readonly UrlEncoder _urlEncoder;

        private const string AuthenticatorUriFormat =
            "otpauth://totp/{0}:{1}?secret={2}&issuer={0}&digits=6";

        public ResetAuthenticatorModel(
            UserManager<IdentityUser> userManager,
            SignInManager<IdentityUser> signInManager,
            ILogger<ResetAuthenticatorModel> logger,
            UrlEncoder urlEncoder)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _logger = logger;
            _urlEncoder = urlEncoder;
        }

        [TempData]
        public string StatusMessage { get; set; }

        [BindProperty]
        public InputModel Input { get; set; }

        public class InputModel
        {
            [Required]
            [StringLength(7, MinimumLength = 6)]
            [DataType(DataType.Text)]
            [Display(Name = "Aktueller Verifizierungscode")]
            public string CurrentCode { get; set; }
        }

        public async Task<IActionResult> OnGetAsync()
        {
            // Einfach Seite anzeigen; Schlüssel wird erst beim Post neu generiert
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
                return Page();

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return NotFound("Benutzer konnte nicht geladen werden.");

            // vorhandenen Code prüfen
            var isValid = await _userManager.VerifyTwoFactorTokenAsync(
                user,
                _userManager.Options.Tokens.AuthenticatorTokenProvider,
                Input.CurrentCode.Replace(" ", "").Replace("-", ""));

            if (!isValid)
            {
                ModelState.AddModelError(nameof(Input.CurrentCode), "Ungültiger Verifizierungscode.");
                return Page();
            }

            // Schlüssel zurücksetzen
            await _userManager.ResetAuthenticatorKeyAsync(user);
            await _signInManager.RefreshSignInAsync(user);
            _logger.LogInformation("User {UserId} hat seinen Authenticator-Schlüssel zurückgesetzt.",
                await _userManager.GetUserIdAsync(user));

            StatusMessage = "Ihr Authenticator-Schlüssel wurde zurückgesetzt. Bitte richten Sie Ihre App neu ein.";
            return RedirectToPage("./EnableAuthenticator");
        }
    }
}
