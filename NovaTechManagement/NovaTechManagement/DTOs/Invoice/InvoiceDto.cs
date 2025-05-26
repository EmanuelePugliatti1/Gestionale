using NovaTechManagement.DTOs.Client; // For ClientDto
// using NovaTechManagement.DTOs.Order;   // For OrderDto (or a simpler version) - Removed as ReturnOrderForInvoiceDto is local
using System;

namespace NovaTechManagement.DTOs.Invoice
{
    public class ReturnOrderForInvoiceDto // A simpler Order DTO for embedding in InvoiceDto
    {
        public int Id { get; set; }
        public DateTime OrderDate { get; set; }
        // Add other relevant order fields if needed, but keep it lean.
    }

    public class InvoiceDto
    {
        public int Id { get; set; }
        public DateTime InvoiceDate { get; set; }
        public int OrderId { get; set; }
        public ReturnOrderForInvoiceDto? Order { get; set; } // Embed basic order details
        public int ClientId { get; set; }
        public ClientDto? Client { get; set; } // Embed client details
        public decimal TotalAmount { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime? DueDate { get; set; }
    }
}
