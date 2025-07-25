using IIS.Ftp.SimpleAuth.Core.Domain;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace IIS.FTP.ManagementWeb.Models
{
    public class UserViewModel
    {
        [Required(ErrorMessage = "User ID is required")]
        [Display(Name = "User ID")]
        [RegularExpression(@"^[a-zA-Z0-9_\-\.]+$", ErrorMessage = "User ID can only contain letters, numbers, underscores, hyphens, and dots")]
        public string UserId { get; set; }

        [Required(ErrorMessage = "Display Name is required")]
        [Display(Name = "Display Name")]
        public string DisplayName { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        [StringLength(100, ErrorMessage = "The {0} must be at least {2} characters long.", MinimumLength = 8)]
        public string Password { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "Confirm Password")]
        [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
        public string ConfirmPassword { get; set; }

        [Display(Name = "Home Directory")]
        [Required(ErrorMessage = "Home Directory is required")]
        public string HomeDirectory { get; set; }

        public List<PermissionViewModel> Permissions { get; set; } = new List<PermissionViewModel>();
    }

    public class PermissionViewModel
    {
        public string Path { get; set; }
        public bool Read { get; set; }
        public bool Write { get; set; }
    }

    public class UserListViewModel
    {
        public List<UserListItemViewModel> Users { get; set; } = new List<UserListItemViewModel>();
        public string SearchTerm { get; set; }
        public int CurrentPage { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public int TotalCount { get; set; }
    }

    public class UserListItemViewModel
    {
        public string UserId { get; set; }
        public string DisplayName { get; set; }
        public string HomeDirectory { get; set; }
        public int PermissionCount { get; set; }
    }

    public class ChangePasswordViewModel
    {
        [Required]
        public string UserId { get; set; }

        [Required(ErrorMessage = "Current password is required")]
        [DataType(DataType.Password)]
        [Display(Name = "Current Password")]
        public string CurrentPassword { get; set; }

        [Required(ErrorMessage = "New password is required")]
        [DataType(DataType.Password)]
        [Display(Name = "New Password")]
        [StringLength(100, ErrorMessage = "The {0} must be at least {2} characters long.", MinimumLength = 8)]
        public string NewPassword { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "Confirm New Password")]
        [Compare("NewPassword", ErrorMessage = "The new password and confirmation password do not match.")]
        public string ConfirmPassword { get; set; }
    }
} 