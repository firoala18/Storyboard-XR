// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#nullable disable

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ProjectsWebApp.DataAccsess.Data;
using System;

namespace ProjectsWebApp.Areas.Identity.Pages.Account
{
    /// <summary>
    ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
    ///     directly from your code. This API may change or be removed in future releases.
    /// </summary>
    [AllowAnonymous]
    public class LockoutModel : PageModel
    {
        private readonly ApplicationDbContext _db;

        public LockoutModel(ApplicationDbContext db)
        {
            _db = db;
        }

        public bool IsManualLockout { get; private set; }

        public string SupportEmail { get; private set; }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public void OnGet()
        {
            var manual = TempData["ManualLockout"] as string;
            IsManualLockout = string.Equals(manual, "true", StringComparison.OrdinalIgnoreCase);

            SupportEmail = _db.ContactEmail
                .OrderBy(e => e.Id)
                .Select(e => e.Email)
                .FirstOrDefault() ?? string.Empty;
        }
    }
}
