using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace NovaTechManagement.DTOs.Order
{
    public class CreateOrderDto
    {
        [Required]
        public int ClientId { get; set; }

        [Required]
        public string Status { get; set; } = "Open"; // Default status

        [Required]
        [MinLength(1, ErrorMessage = "Order must contain at least one item.")]
        public List<OrderItemDto> OrderItems { get; set; } = new List<OrderItemDto>();
    }
}
