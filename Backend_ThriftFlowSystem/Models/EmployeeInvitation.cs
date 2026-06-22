using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Backend_ThriftFlowSystem.Models
{
    [Index(nameof(InvitationToken), IsUnique = true)]
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

        public int InvitedByEmployeeId { get; set; }

        [ForeignKey(nameof(InvitedByEmployeeId))]
        public Employee? InvitedBy { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
