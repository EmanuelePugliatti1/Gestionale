using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace NovaTechManagement.DTOs.Order
{
    public class UpdateOrderDto
    {
        public int? ClientId { get; set; } // Client usually doesn't change post-creation, but allow if needed
        public string? Status { get; set; } // e.g., "Processing", "Shipped", "Completed", "Closed"
        // Updating order items can be complex (add, remove, update quantity).
        // For this iteration, focus on status updates. Full item updates can be a V2 feature or a separate endpoint.
        // public List<OrderItemDto>? OrderItems { get; set; }
    }
}
