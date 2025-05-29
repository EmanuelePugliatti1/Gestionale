using System;
using System.ComponentModel.DataAnnotations;

namespace NovaTechManagement.DTOs.Invoice
{
    public class UpdateInvoiceDto
    {
        public string? Status { get; set; } // e.g., "Pending", "Paid", "Overdue"
        public DateTime? DueDate { get; set; }
        // Other fields like OrderId, ClientId, TotalAmount are generally not updatable for an invoice.
    }
}
