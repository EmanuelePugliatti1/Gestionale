using System.ComponentModel.DataAnnotations;

namespace NovaTechManagement.DTOs.Order
{
    public class OrderItemDto
    {
        [Required]
        public int ProductId { get; set; }

        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "Quantity must be at least 1.")]
        public int Quantity { get; set; }

        // Price can be omitted here if it's always taken from the Product table at the time of order creation.
        // If price can be overridden at order time, add it here. For now, assume it's fetched from Product.
    }
}
