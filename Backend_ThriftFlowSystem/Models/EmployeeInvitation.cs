using System.ComponentModel.DataAnnotations;

namespace Backend_ThriftFlowSystem.Models
{
    public class EmployeeInvitation
    {
        [Key]
        public Guid Id { get; set; } // Unique identifier for the invitation

        [Required]
        [EmailAddress]
        [MaxLength(150)]
        public string Email { get; set; } = string.Empty;

        [Required]
        public int RoleId { get; set; } 

        [Required]
        public string InvitationToken { get; set; } = string.Empty;

        public DateTime ExpiresAt { get; set; } 

        public bool IsUsed { get; set; } = false; // Check if the invitation has been used

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
