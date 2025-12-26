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
using QRCoder;

namespace ProjectsWebApp.Areas.Identity.Pages.Account.Manage
{
    public class EnableAuthenticatorModel : PageModel
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly ILogger<EnableAuthenticatorModel> _logger;
        private readonly UrlEncoder _urlEncoder;
        private const string AuthenticatorUriFormat =
            "otpauth://totp/{0}:{1}?secret={2}&issuer={0}&digits=6";

        public EnableAuthenticatorModel(
            UserManager<IdentityUser> userManager,
            ILogger<EnableAuthenticatorModel> logger,
            UrlEncoder urlEncoder)
        {
            _userManager = userManager;
            _logger = logger;
            _urlEncoder = urlEncoder;
        }

        [TempData]
        public string StatusMessage { get; set; }

        /// <summary>
        /// Form input for the verification code.
        /// </summary>
        [BindProperty]
        public InputModel Input { get; set; }

        public class InputModel
        {
            [Required]
            [StringLength(7, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 6)]
            [DataType(DataType.Text)]
            [Display(Name = "Verification Code")]
            public string Code { get; set; }
        }

        /// <summary>
        /// This is the fallback key if someone really wants to type it manually.
        /// </summary>
        public string SharedKey { get; set; }

        /// <summary>
        /// The URI that we push into the QR code generator.
        /// </summary>
        public string AuthenticatorUri { get; set; }

        /// <summary>
        /// A `data:image/png;base64,...` string ready to plug into an `<img>`.
        /// </summary>
        public string QrCodeImageUrl { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");

            await LoadSharedKeyAndQrCodeUriAsync(user);
            GenerateQrCodeImage();   // ← generate the base64 image
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");

            if (!ModelState.IsValid)
            {
                await LoadSharedKeyAndQrCodeUriAsync(user);
                GenerateQrCodeImage();
                return Page();
            }

            var verificationCode = Input.Code
                .Replace(" ", string.Empty)
                .Replace("-", string.Empty);

            var is2faTokenValid = await _userManager.VerifyTwoFactorTokenAsync(
                user, _userManager.Options.Tokens.AuthenticatorTokenProvider, verificationCode);

            if (!is2faTokenValid)
            {
                ModelState.AddModelError("Input.Code", "Verification code is invalid.");
                await LoadSharedKeyAndQrCodeUriAsync(user);
                GenerateQrCodeImage();
                return Page();
            }

            await _userManager.SetTwoFactorEnabledAsync(user, true);
            _logger.LogInformation("User with ID '{UserId}' has enabled 2FA.", await _userManager.GetUserIdAsync(user));
            StatusMessage = "Your authenticator app has been verified.";

            // If no recovery codes, generate them now:
            if (await _userManager.CountRecoveryCodesAsync(user) == 0)
            {
                var recoveryCodes = await _userManager.GenerateNewTwoFactorRecoveryCodesAsync(user, 10);
                TempData["RecoveryCodes"] = recoveryCodes;
                return RedirectToPage("./ShowRecoveryCodes");
            }

            return RedirectToPage("./TwoFactorAuthentication");
        }

        private async Task LoadSharedKeyAndQrCodeUriAsync(IdentityUser user)
        {
            var unformattedKey = await _userManager.GetAuthenticatorKeyAsync(user);
            if (string.IsNullOrEmpty(unformattedKey))
            {
                await _userManager.ResetAuthenticatorKeyAsync(user);
                unformattedKey = await _userManager.GetAuthenticatorKeyAsync(user);
            }

            SharedKey = FormatKey(unformattedKey);
            var email = await _userManager.GetEmailAsync(user);
            AuthenticatorUri = string.Format(
                CultureInfo.InvariantCulture,
                AuthenticatorUriFormat,
                _urlEncoder.Encode("ProjectsWebApp"),
                _urlEncoder.Encode(email),
                unformattedKey);
        }

        private static string FormatKey(string key)
        {
            var result = new StringBuilder();
            for (int i = 0; i < key.Length; i += 4)
            {
                if (i + 4 < key.Length)
                    result.Append(key.Substring(i, 4)).Append(' ');
                else
                    result.Append(key.Substring(i));
            }
            return result.ToString().ToLowerInvariant();
        }

        private void GenerateQrCodeImage()
        {
            var qrGenerator = new QRCodeGenerator();
            using var qrCodeData = qrGenerator.CreateQrCode(AuthenticatorUri, QRCodeGenerator.ECCLevel.Q);
            var base64Qr = new Base64QRCode(qrCodeData).GetGraphic(20);
            QrCodeImageUrl = $"data:image/png;base64,{base64Qr}";
        }
    }
}
