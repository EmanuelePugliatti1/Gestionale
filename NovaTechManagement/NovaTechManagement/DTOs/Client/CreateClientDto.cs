using System.ComponentModel.DataAnnotations;

namespace NovaTechManagement.DTOs.Client
{
    public class CreateClientDto
    {
        [Required]
        public string ClientName { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        public string? Phone { get; set; }

        [Required]
        public string Status { get; set; } = string.Empty; // e.g., "Active", "Inactive"
    }
}
