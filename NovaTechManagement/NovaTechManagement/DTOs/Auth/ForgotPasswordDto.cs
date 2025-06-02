using System.ComponentModel.DataAnnotations;

namespace NovaTechManagement.DTOs.Auth
{
    public class ForgotPasswordDto
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty; // Initialize here
    }
}
