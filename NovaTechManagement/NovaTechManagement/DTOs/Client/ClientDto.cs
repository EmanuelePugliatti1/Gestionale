using System;

namespace NovaTechManagement.DTOs.Client
{
    public class ClientDto
    {
        public int Id { get; set; }
        public string ClientName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime DateAdded { get; set; }
    }
}
