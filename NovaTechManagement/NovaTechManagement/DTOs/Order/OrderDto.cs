using NovaTechManagement.DTOs.Client; // For ClientDto
using System;
using System.Collections.Generic;

namespace NovaTechManagement.DTOs.Order
{
    public class ReturnOrderItemDto // Specific DTO for returning OrderItems with product details
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty; // Include product name
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; } // Price at the time of order
        public decimal TotalPrice => Quantity * UnitPrice;
    }

    public class OrderDto
    {
        public int Id { get; set; }
        public DateTime OrderDate { get; set; }
        public int ClientId { get; set; }
        public ClientDto? Client { get; set; } // Embed client details
        public string Status { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
        public List<ReturnOrderItemDto> OrderItems { get; set; } = new List<ReturnOrderItemDto>();
    }
}
