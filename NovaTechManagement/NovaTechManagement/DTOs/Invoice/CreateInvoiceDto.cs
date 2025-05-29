using System;
using System.ComponentModel.DataAnnotations;

namespace NovaTechManagement.DTOs.Invoice
{
    public class CreateInvoiceDto
    {
        [Required]
        public int OrderId { get; set; }

        // InvoiceDate will be set to DateTime.UtcNow on creation.
        // ClientId and TotalAmount will be derived from the Order.

        [Required]
        public string Status { get; set; } = "Pending"; // e.g., "Pending", "Paid", "Overdue"

        public DateTime? DueDate { get; set; }
    }
}
