using System.ComponentModel.DataAnnotations;

namespace NovaTechManagement.DTOs.Client
{
    public class UpdateClientDto
    {
        public string? ClientName { get; set; }

        [EmailAddress]
        public string? Email { get; set; }

        public string? Phone { get; set; }

        public string? Status { get; set; }
    }
}
