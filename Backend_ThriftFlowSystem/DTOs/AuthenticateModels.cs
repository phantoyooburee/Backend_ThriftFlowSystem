using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Backend_ThriftFlowSystem.DTOs
{
    public class AuthenticateModels
    {

        // DTO for Owner Invite Employee (InviteEmployeeRequest)
        public class InviteEmployeeRequest
        {
            [Required(ErrorMessage = "Email is required"), EmailAddress(ErrorMessage = "Email format is incorrect.")]
            public string? Email { get; set; }

            [Required(ErrorMessage = "RoleId is required")]
            public int RoleId { get; set; }
        }

        public class RegisterRequest
        {
            //[Required(ErrorMessage = "Invitation token is required")]
            public string? InvitationToken { get; set; }

            [EmailAddress(
            ErrorMessage = "Email format is incorrect."), StringLength(150)]
            public string? Email { get; set; }

            [Required(ErrorMessage = "Username is required")]
            [StringLength(50), RegularExpression(@"^[A-Za-z0-9._-]{3,50}$",
             ErrorMessage = "Username must be 3–50 chars using letters, numbers, dot, underscore, or hyphen.")]
            [DefaultValue("Tanguay.Admin")]
            public string? Username { get; set; }

            [Required, RegularExpression(
            @"^[a-zA-Z0-9!@#$%^&*()_\-+=\[{\]};:'"",<.>/?\\|`~]{8,}$", ErrorMessage = "Password must be at least 8 characters and contain only letters, numbers, and special characters (!@#$%^&* etc.) ")]
            public string? Password { get; set; }

            [Required(ErrorMessage = "PIN is required")]
            [RegularExpression(@"^[0-9]{6}$", ErrorMessage = "PIN must be exactly 6 digits.")]
            public string? Pin { get; set; }

            [StringLength(100)]
            [RegularExpression(@"^[\p{L}\p{M}\s]*$", ErrorMessage = "First name can contain letter only.")]
            [DefaultValue("Tanguay")]
            public string? FirstName { get; set; }

            [StringLength(100)]
            [RegularExpression(@"^[\p{L}\p{M}\s]*$", ErrorMessage = "Last name can contain letters only.")]
            [DefaultValue("Saelee")]
            public string? LastName { get; set; }
        }

        public class LoginRequest
        {
            [Required]
            public string? Username { get; set; }

            [Required]
            public string? Password { get; set; }
        }

        public class AuthResponse
        {
            public int Id { get; set; }
            public string? Username { get; set; }
            public string? Email { get; set; }
            public string? FirstName { get; set; }
            public string? LastName { get; set; }
            public string? RoleName { get; set; }
            public bool IsFirstLogin { get; set; }
            public string? Token { get; set; }
        }

        public class ForgotPasswordRequest
        {
            [Required(ErrorMessage = "Email is required"), EmailAddress(
            ErrorMessage = "Email format is incorrect."), StringLength(100)]
            public string? Email { get; set; }
        }

        public class ResetPasswordRequest
        {
            [Required(ErrorMessage = "Token is required")]
            public string? Token { get; set; }

            [Required, RegularExpression(
            @"^[a-zA-Z0-9!@#$%^&*()_\-+=\[{\]};:'"",<.>/?\\|`~]{8,}$",
            ErrorMessage = "Password must be at least 8 characters and contain only letters, numbers, and special characters (!@#$%^&* etc.) ")]
            public string Password { get; set; } = string.Empty;

            [Required(ErrorMessage = "Confirm Password is required")]
            [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
            public string ConfirmPassword { get; set; } = string.Empty;
        }
    }
}
