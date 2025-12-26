using System;
using System.ComponentModel.DataAnnotations;

namespace ProjectsWebApp.Models
{
    public class UserSecurityState
    {
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;

        public int LockoutCount { get; set; }

        public bool IsManuallyLocked { get; set; }

        public DateTime? LastLockoutUtc { get; set; }
    }

    public class LockedAccountViewModel
    {
        public string UserId { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public int LockoutCount { get; set; }
        public DateTimeOffset? LockoutEnd { get; set; }
    }
}
